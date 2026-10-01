using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>
/// Reads the method out of a method group such as <c>provider => provider.TrackAsync</c>. The compiler
/// turns that conversion into a <c>CreateDelegate</c> call on a constant <see cref="MethodInfo"/>.
/// </summary>
internal static class MethodGroup
{
    public static MethodInfo Resolve(LambdaExpression expression, Type ownerType)
    {
        if (Unwrap(expression.Body) is MethodCallExpression { Method.Name: nameof(MethodInfo.CreateDelegate) } call
            && FindMethod(call) is { DeclaringType: { } declaringType } method
            && declaringType.IsAssignableFrom(ownerType)
        )
        {
            return method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
        }

        throw new ArgumentException(
            $"'{expression}' is not a method group of '{ownerType.Name}', such as 'provider => provider.TrackAsync'.",
            nameof(expression)
        );
    }

    /// <summary>
    /// Compares a service method with its replacement. Type parameters of generic methods are matched by
    /// position, and the replacement must accept every type argument the service method accepts.
    /// </summary>
    public static bool HaveSameSignature(MethodInfo method, MethodInfo replacement)
        => method.GetGenericArguments().Length == replacement.GetGenericArguments().Length
            && AreSameType(method.ReturnType, replacement.ReturnType)
            && method.GetParameters().Length == replacement.GetParameters().Length
            && method.GetParameters()
                .Zip(replacement.GetParameters())
                .All(pair => AreSameType(pair.First.ParameterType, pair.Second.ParameterType))
            && method.GetGenericArguments()
                .Zip(replacement.GetGenericArguments())
                .All(pair => Accepts(pair.Second, pair.First)
        );

    private static bool AreSameType(Type type, Type other)
    {
        if (type.IsGenericMethodParameter || other.IsGenericMethodParameter)
        {
            return type.IsGenericMethodParameter
                && other.IsGenericMethodParameter
                && type.GenericParameterPosition == other.GenericParameterPosition;
        }

        if (type.HasElementType || other.HasElementType)
        {
            return type.HasElementType
                && other.HasElementType
                && type.IsArray == other.IsArray
                && type.IsByRef == other.IsByRef
                && type.IsPointer == other.IsPointer
                && (!type.IsArray || type.GetArrayRank() == other.GetArrayRank())
                && AreSameType(type.GetElementType()!, other.GetElementType()!);
        }

        if (type.IsGenericType && other.IsGenericType)
        {
            return type.GetGenericTypeDefinition() == other.GetGenericTypeDefinition()
                && type.GetGenericArguments()
                    .Zip(other.GetGenericArguments())
                    .All(pair => AreSameType(pair.First, pair.Second));
        }

        return type == other;
    }

    private static bool Accepts(Type replacementParameter, Type serviceParameter)
    {
        var extraConstraints = replacementParameter.GenericParameterAttributes
            & GenericParameterAttributes.SpecialConstraintMask
            & ~serviceParameter.GenericParameterAttributes;

        var serviceConstraints = serviceParameter.GetGenericParameterConstraints();

        return extraConstraints == GenericParameterAttributes.None
            && replacementParameter
                .GetGenericParameterConstraints()
                .All(constraint => serviceConstraints.Any(serviceConstraint => AreSameType(constraint, serviceConstraint)));
    }

    private static Expression Unwrap(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion
            ? Unwrap(conversion.Operand)
            : expression;

    private static MethodInfo? FindMethod(MethodCallExpression call)
        => new[] { call.Object }
            .Concat(call.Arguments)
            .OfType<ConstantExpression>()
            .Select(constant => constant.Value)
            .OfType<MethodInfo>()
            .FirstOrDefault();
}
