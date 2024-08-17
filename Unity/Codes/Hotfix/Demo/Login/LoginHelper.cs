using System;


namespace ET
{
    [FriendClass(typeof(AccountInfoComponent))]
    public static class LoginHelper
    {
        public static async ETTask<int> Login(Scene zoneScene, string address, string account, string password)
        {
            A2C_LoginAccount a2CLoginAccount = null;
            Session accountSession = null;
            try
            {
                password = MD5Helper.StringMD5(password);
                accountSession = zoneScene.GetComponent<NetKcpComponent>().Create(NetworkHelper.ToIPEndPoint(address));
                a2CLoginAccount = (A2C_LoginAccount) await accountSession.Call(new C2A_LoginAccount(){AccountName = account, Password = password});
            }
            catch (Exception e)
            {
                accountSession?.Dispose();
                Log.Error(e.ToString());
                return ErrorCode.ERR_NetworkError;
            }

            if (a2CLoginAccount.Error != ErrorCode.ERR_Success)
            {
                accountSession?.Dispose();
                return a2CLoginAccount.Error;
            }

            zoneScene.AddComponent<SessionComponent>().Session = accountSession;
            zoneScene.GetComponent<SessionComponent>().Session.AddComponent<PingComponent>();
            
            zoneScene.GetComponent<AccountInfoComponent>().Token = a2CLoginAccount.Token;
            zoneScene.GetComponent<AccountInfoComponent>().AccountId = a2CLoginAccount.AccountId;
            
            return ErrorCode.ERR_Success;
        }

        public static async ETTask<int> GetServerInfos(Scene zoneScene)
        {
            A2C_GetServerInfos getServerInfos = null;
            try
            {
                getServerInfos =  (A2C_GetServerInfos) await zoneScene.GetComponent<SessionComponent>().Session.Call( new C2A_GetServerInfos()
                        {
                            AccountId = zoneScene.GetComponent<AccountInfoComponent>().AccountId,
                            Token = zoneScene.GetComponent<AccountInfoComponent>().Token,       
                        });
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return ErrorCode.ERR_NetworkError;
            }

            if (getServerInfos.Error != ErrorCode.ERR_Success)
            {
                return getServerInfos.Error;
            }

            foreach (var serverInfoProto in getServerInfos.ServerInfosList)
            {
                ServerInfo serverInfo = zoneScene.GetComponent<ServerInfosComponent>().AddChild<ServerInfo>();
                serverInfo.FromMessage(serverInfoProto);
                zoneScene.GetComponent<ServerInfosComponent>().Add(serverInfo);
            }
            
            
            return ErrorCode.ERR_Success;
        }

        public static async ETTask<int> CreateRole(Scene zoneScene, string name)
        {
            A2C_CreateRole a2CCreateRole = null;
            try
            {
                a2CCreateRole = (A2C_CreateRole)await zoneScene.GetComponent<SessionComponent>().Session.Call(new C2A_CreateRole() 
                        {
                            AccountId = zoneScene.GetComponent<AccountInfoComponent>().AccountId,
                            Token = zoneScene.GetComponent<AccountInfoComponent>().Token,
                            Name = name,
                            ServerId = 1,
                        });

            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return ErrorCode.ERR_NetworkError;
            }

            if (a2CCreateRole.Error != ErrorCode.ERR_Success)
            {
                Log.Error(a2CCreateRole.Error.ToString());
                return a2CCreateRole.Error;
            }
                
            
            await ETTask.CompletedTask;
            return ErrorCode.ERR_Success;
        }
    }
}