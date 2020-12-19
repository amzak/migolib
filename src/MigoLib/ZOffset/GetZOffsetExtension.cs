namespace MigoLib.ZOffset
{
    public static class GetZOffsetExtension
    {
        public static CommandChain GetZOffset(this CommandChain self)
        {
            var command = new GetZOffsetCommand();
            self.Append(command);
            return self;
        }
    }
}