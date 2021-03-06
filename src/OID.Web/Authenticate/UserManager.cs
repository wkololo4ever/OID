﻿using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using OID.Core;

namespace OID.Web.Authenticate
{
    public class UserManager : IUserManager
    {
        private readonly ISession _session;

        const string USER_KEY = "USER_KEY";

        public UserManager(IHttpContextAccessor contextAccessor)
        {
            _session = contextAccessor.HttpContext.Session;
        }

        public UserManager(HttpContext context)
        {
            _session = context.Session;
        }

        public void SetUser(UserModel userModel)
        {
            _session.SetString(USER_KEY, JsonConvert.SerializeObject(userModel));
        }

        public UserModel GetUser()
        {
            var user = _session.GetString(USER_KEY);
            if (user != null)
            {
                return JsonConvert.DeserializeObject<UserModel>(user);
            }

            return null;
        }

        public void UpdateSessionId(string sessionId)
        {
            var user = GetUser();
            if (user != null)
            {
                user.SessionId = sessionId;
                SetUser(user);
            }
        }

        public bool Authenticated()
        {
            return GetUser() != null;
        }

        public void RemoveUser()
        {
            _session.Remove(USER_KEY);
        }
    }
}
