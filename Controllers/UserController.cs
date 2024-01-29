using JWT_WEBAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JWT_WEBAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly EmployeeDBContext context;
        private readonly JWTSetting setting;
        private readonly IRefreshTokenGenerator tokenGenerator;
        public UserController(EmployeeDBContext employeedb,IOptions<JWTSetting> options,IRefreshTokenGenerator _refreshToken  )
        {
            context = employeedb;
            setting = options.Value;
            tokenGenerator = _refreshToken;
        }


        public TokenResponse Authenticate(string username, Claim[] claims)
        {
            TokenResponse tokenResponse = new TokenResponse();
            var tokenkey = Encoding.UTF8.GetBytes(setting.securitykey);
            var tokenhandler = new JwtSecurityToken(
                
                claims: claims,
                expires:DateTime.Now.AddMinutes(2),
                signingCredentials:new SigningCredentials(new SymmetricSecurityKey(tokenkey),SecurityAlgorithms.HmacSha256)
                );
            tokenResponse.JWTToken = new JwtSecurityTokenHandler().WriteToken(tokenhandler);
            tokenResponse.RefreshToken = tokenGenerator.GenerateToken(username);
            return tokenResponse;
        }


        [Route("Authenticate")]
        [HttpPost]
        public IActionResult Authenticate([FromBody] usercred user)
        {

            TokenResponse tokenResponse = new TokenResponse();

            var _user = context.TblUser.FirstOrDefault(o => o.Userid == user.username && o.Password==user.password);
            if (_user == null)
            
                return Unauthorized();
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenkey = Encoding.UTF8.GetBytes(setting.securitykey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    new Claim[]
                    {
                        new Claim(ClaimTypes.Name, _user.Userid),
                        new Claim(ClaimTypes.Role, _user.Role)

                    }
                    ),
             
                Expires = DateTime.UtcNow.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256)

            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            string finaltoken = tokenHandler.WriteToken(token);

            tokenResponse.JWTToken = finaltoken;
            tokenResponse.RefreshToken = tokenGenerator.GenerateToken(user.username);

            return Ok(tokenResponse);
        }
        [Route("Refresh")]
        [HttpPost]
        public IActionResult Refresh([FromBody] TokenResponse token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token.JWTToken, new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(setting.securitykey)),
                ValidateIssuer = false,
                ValidateAudience = false

            }, out securityToken);


            var _token = securityToken as JwtSecurityToken;
            if (_token!=null && !_token.Header.Alg.Equals(SecurityAlgorithms.HmacSha256))
            {
                return Unauthorized();
            }
            var username = principal.Identity.Name;
            var _reftable = context.TblRefreshtoken.FirstOrDefault(o => o.UserId == username && o.RefreshToken == token.RefreshToken);
            if (_reftable == null)
            {
                return Unauthorized();
            }
            TokenResponse _result = Authenticate(username, principal.Claims.ToArray());
            return Ok(_result);
        }


    }
}
