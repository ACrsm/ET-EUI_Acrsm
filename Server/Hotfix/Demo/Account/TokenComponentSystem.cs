namespace ET
{
    [FriendClass(typeof(TokenComponent))]
    public static class TokenComponentSystem
    {
        public static void Add(this TokenComponent self, long key, string token)
        {
            self.TokenDictionary.Add(key, token);
            self.TimeOutRemoveKey(key, token).Coroutine();
        }

        public static string Get(this TokenComponent self, long key)
        {
            self.TokenDictionary.TryGetValue(key, out string token);
            return token;
        }

        public static void Remove(this TokenComponent self, long key)
        {
            self.TokenDictionary.Remove(key);
        }

        public static async ETTask TimeOutRemoveKey(this TokenComponent self, long key, string token)
        {
            await TimerComponent.Instance.WaitAsync(10 * 3600);
            string onlineToken = self.Get(key);
            if (!string.IsNullOrEmpty(onlineToken) && (onlineToken == token))
            {
                self.Remove(key);
            }
        }
    }
}