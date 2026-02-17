// <copyright file="SemaphoreSlimExt.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading;

namespace FubarDev.FtpServer
{
    internal class SemaphoreSlimExt : SemaphoreSlim
    {
        public SemaphoreSlimExt(int initialCount)
            : base(initialCount)
        {
        }
        public SemaphoreSlimExt(int initialCount, int maxCount)
            : base(initialCount, maxCount)
        {
        }

        public bool IsDisposed { get; internal set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            IsDisposed = true;
        }
    }
}
