﻿using System.Threading.Tasks;
using OID.DataProvider.Models;
using OID.DataProvider.Models.User;
using OID.DataProvider.Models.User.In;

namespace OID.DataProvider.Interfaces
{
    public interface IUserProvider
    {
        Task<DataProviderModel<CreatedUserModel>> CreateUser(string email, string userName, string passwordHash);

        Task<DataProviderModel<SessionModel>> UpdateUser(UserModel userModel, UserProfileModel oldProfile, UserProfileModel newProfile);

        Task<DataProviderModel<SessionModel>> ChangePassword(UserModel userModel, string oldPasswordHash, string newPasswordHash);
    }
}