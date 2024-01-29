using JWT_WEBAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace JWT_WEBAPI
{
    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {

        private readonly EmployeeDBContext context;

        public RefreshTokenGenerator(EmployeeDBContext employeedb)
        {
            context = employeedb;
        }
        public string GenerateToken(string username)
        {
            var randomnumber = new byte[32];
            var name = "naresh";
            using (var randomnumbergenerator=RandomNumberGenerator.Create())
            {
                randomnumbergenerator.GetBytes(randomnumber);
                string RefreshToken = Convert.ToBase64String(randomnumber);

                var _user = context.TblRefreshtoken.FirstOrDefault(o=>o.UserId==username);
                if (_user != null)
                {
                    _user.RefreshToken = RefreshToken;
                    context.SaveChanges();

                }
                else
                {
                    TblRefreshtoken tblRefreshtoken = new TblRefreshtoken()
                    {
                        UserId = username,
                        TokenId = new Random().Next().ToString(),
                        RefreshToken = RefreshToken,
                        IsActive = true

                    };
                }

                return RefreshToken;
            }
        }
    }
}
