namespace Microsoft.FeatureManagement.Utilities.Tests;

public static class PrinterDoubles
{
    public interface IReceiptPrinter : IDisposable, IAsyncDisposable
    {
        string Print();
    }

    public sealed class ReceiptPrinter : IReceiptPrinter
    {
        public int Disposals { get; private set; }

        public string Print()
            => "printed";

        public void Dispose()
            => Disposals++;

        public ValueTask DisposeAsync()
        {
            Disposals++;

            return ValueTask.CompletedTask;
        }
    }
}
