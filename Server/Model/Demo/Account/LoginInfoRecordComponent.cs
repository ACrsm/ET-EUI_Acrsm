using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class LoginInfoRecordComponent : Entity, IAwake, IDestroy
    {
        //key为accountId， value为区服zone
        public Dictionary<long, int> AccountLoginInfoDict = new Dictionary<long, int>();
    }
}