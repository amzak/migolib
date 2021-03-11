using System;

namespace MigoLib.Tests
{
    [Flags]
    public enum FakeMigoMode
    {
        Reply,
        Request,
        Stream,
        RequestReply = Request | Reply
    }
}