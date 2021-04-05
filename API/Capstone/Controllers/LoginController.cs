﻿using Microsoft.AspNetCore.Mvc;
using Capstone.DAO;
using Capstone.Models;
using Capstone.Security;

namespace Capstone.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ITokenGenerator tokenGenerator;
        private readonly IPasswordHasher passwordHasher;
        private readonly IUserDAO userDAO;

        public LoginController(ITokenGenerator _tokenGenerator, IPasswordHasher _passwordHasher, IUserDAO _userDAO)
        {
            tokenGenerator = _tokenGenerator;
            passwordHasher = _passwordHasher;
            userDAO = _userDAO;
        }

        [HttpPost]
        public IActionResult Authenticate(LoginUser userParam)
        {
            // Default to bad username/password message
            IActionResult result = Unauthorized(new { message = "Username or password is incorrect" });

            // Get the user by email
            User user = userDAO.GetUser(userParam.Email);

            // If we found a user and the password hash matches
            if (user != null && passwordHasher.VerifyHashMatch(user.PasswordHash, userParam.Password, user.Salt))
            {
                // Create an authentication token
                string token = tokenGenerator.GenerateToken(user.UserId, user.Username, user.Email, user.Role);

                // Create a ReturnUser object to return to the client
                LoginResponse retUser = new LoginResponse() { User = new ReturnUser() { UserId = user.UserId, Username = user.Username, Email = user.Email, Role = user.Role }, Token = token };

                // Switch to 200 OK
                result = Ok(retUser);
            }

            return result;
        }

        [HttpPost("/register")]
        public IActionResult Register(RegisterUser userParam)
        {
            IActionResult result;

            User existingUser = userDAO.GetUser(userParam.Email);
            if (existingUser != null)
            {
                if(existingUser.Email == userParam.Email)
                {
                    return Conflict(new { message = "Email already registered. Please login or register with a new email." });
                }
                if (existingUser.Username == userParam.Username)
                {
                    return Conflict(new { message = "Username taken. Please choose a different username." });
                }
                else
                {
                    return Conflict(new { message = "User already exists." });
                }
                

            }

            User user = userDAO.AddUser(userParam.Email, userParam.Username, userParam.Password, userParam.Role);
            if (user != null)
            {
                result = Created(user.Email, null); //values aren't read on client
            }
            else
            {
                result = BadRequest(new { message = "An error occurred and user was not created." });
            }

            return result;
        }
    }
}
