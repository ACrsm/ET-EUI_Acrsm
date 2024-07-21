using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommandLine;

namespace ET
{
    [FriendClass(typeof(Account))]
    public class C2A_LoginAccountHandler : AMRpcHandler<C2A_LoginAccount, A2C_LoginAccount>
    {
        protected override async ETTask Run(Session session, C2A_LoginAccount request, A2C_LoginAccount response, Action reply)
        {
            if (session.DomainScene().SceneType != SceneType.Account)
            {
                Log.Error($"C2A_LoginAccount 请求的Scene = {session.DomainScene().SceneType} 错误， 应该是Account");
                session.Dispose();
                return;
            }
            
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            if (session.GetComponent<SessionLockingComponent>() != null)
            {
                response.Error = ErrorCode.ERR_RequestRepeatedly;
                reply();
                session.Disconnect().Coroutine();
                return;
            }
            
            if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
            {
                response.Error = ErrorCode.ERR_LoginInfoIsNull; 
                reply();
                session.Disconnect().Coroutine();
                return;
            }
            
            if (!Regex.IsMatch(request.AccountName.Trim(),@"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_AccountNameFormError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }
   
            if (!Regex.IsMatch(request.Password.Trim(),@"^[A-Za-z0-9]+$"))
            {
                response.Error = ErrorCode.ERR_PasswordFormError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }

            using (session.AddComponent<SessionLockingComponent>())
            {
                using (await CoroutineLockComponent.Instance.Wait(CoroutineLockType.LoginAccount, request.AccountName.Trim().GetHashCode()))
                {
                    List<Account> accountInfoList = await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Query<Account>(d => d.AccountName.Equals(request.AccountName.Trim()));
                    Account account = null;
                    if ((accountInfoList != null) && (accountInfoList.Count > 0))
                    {
                        account = accountInfoList[0];
                        if (account.AccountType == (int)AccountType.BlackList)
                        {
                            response.Error = ErrorCode.ERR_AccountIsInBlackList;
                            reply();
                            session.Disconnect().Coroutine();
                            account?.Dispose();
                            return;
                        }

                        if (!account.Password.Equals(request.Password))
                        {
                            response.Error = ErrorCode.ERR_LoginPasswordError;
                            reply();
                            session.Disconnect().Coroutine();
                            account?.Dispose();
                            return;
                        }
                    }
                    else
                    {
                        account = session.AddChild<Account>();
                        account.AccountName = request.AccountName.Trim();
                        account.Password = request.Password.Trim();
                        account.CreateTime = TimeHelper.ServerNow();
                        account.AccountType = (int)AccountType.General;
                        await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Save<Account>(account);
                    }

                    string token = TimeHelper.ServerNow().ToString() + RandomHelper.random.Next(0, int.MaxValue);
                    session.DomainScene().GetComponent<TokenComponent>().Remove(account.Id);
                    session.DomainScene().GetComponent<TokenComponent>().Add(account.Id, token);

                    response.AccountId = account.Id;
                    response.Token = token;
                    reply();
                    account?.Dispose();
                }
            }
        }
    }
}