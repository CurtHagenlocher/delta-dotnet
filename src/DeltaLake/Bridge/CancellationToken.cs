using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DeltaLake.Bridge
{
    /// <summary>
    /// Core-owned cancellation token.
    /// </summary>
    internal sealed class CancellationToken : SafeHandle
    {
        private readonly List<IDisposable> _cancellationRegistrations = new List<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CancellationToken"/> class.
        /// </summary>
        public CancellationToken()
            : base(IntPtr.Zero, true)
        {
            unsafe
            {
                Ptr = Interop.Methods.cancellation_token_new();
                SetHandle((IntPtr)Ptr);
            }
        }

        /// <inheritdoc/>
        public override unsafe bool IsInvalid => false;

        /// <summary>
        /// Gets internal token pointer.
        /// </summary>
        internal unsafe Interop.CancellationToken* Ptr { get; private init; }

        /// <summary>
        /// Create a core cancellation token from the given cancellation token.
        /// </summary>
        /// <param name="token">Threading token.</param>
        /// <returns>Created cancellation token.</returns>
        public static CancellationToken FromThreading(System.Threading.CancellationToken token)
        {
            var ret = new CancellationToken();
            if (token.IsCancellationRequested)
            {
                ret.Cancel();
            }
            else
            {
                ret._cancellationRegistrations.Add(token.Register(ret.Cancel));
            }

            return ret;
        }

        /// <summary>
        /// Cancel this token.
        /// </summary>
        public void Cancel()
        {
            unsafe
            {
                Interop.Methods.cancellation_token_cancel(Ptr);
            }
        }

        /// <inheritdoc/>
        protected override unsafe bool ReleaseHandle()
        {
            Interop.Methods.cancellation_token_free(Ptr);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                foreach (var registration in _cancellationRegistrations)
                {
                    registration.Dispose();
                }
            }
        }
    }
}