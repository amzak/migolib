namespace MigoLib.State
{
    /*
     @#state;0.00;0.00;0;25;0;10;1;0;0;0#@
     */
    public class GetStateParser : ResultParser<MigoStateModel>
    {
        protected override void Setup(PositionalSerializer<MigoStateModel> serializer)
        {
            serializer
                .FixedString("state")
                .Field(x => x.HeadX)
                .Field(x => x.HeadY)
                .Field(x => x.BedTemp)
                .Field(x => x.NozzleTemp);
        }
   }
}