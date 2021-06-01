using System;

namespace MigoLib.Fake
{
    [Flags]
    public enum FakeMigoMode
    {
        Reply,
        Request,
        Stream,
        RealStream,
        RequestReply = Request | Reply
    }
}