using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LoginAndRegistration.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LoginAndRegistration.Controllers
{
    public class HomeController : Controller
    {
        private int? uid
        {
            get 
            {
                return HttpContext.Session.GetInt32("UserId");
            }
        }

        private bool loggedIn
        {
            get 
            {
                return uid != null;
            }
        }
        
        private LogRegContext db;
        public HomeController(LogRegContext context)
        {
            db = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("Index");
        }

        [HttpPost("/Register")]
        public IActionResult Registration(User user)
        {
            if (ModelState.IsValid)
            {
                if(db.Users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!");
                    return View("Index");
                }
                    PasswordHasher<User> Hasher = new PasswordHasher<User>();
                    user.Password = Hasher.HashPassword(user, user.Password);

                    db.Add(user);
                    db.SaveChanges();

                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("FullName", user.FullName());

                    return RedirectToAction("Success");
            }
            return View("Index");;
        }

        [HttpGet("/success")]
        public IActionResult Success()
        {
            if (!loggedIn)
            {
                return RedirectToAction("Index");
            }
            return View("Success");
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Success");
            }
            return View("Login");
        }

        [HttpPost("/loginProcess")]
        public IActionResult LoginProcess(LoginUser loginUser)
        {
            if (ModelState.IsValid == false)
            {
                // Display errors on form.
                return View("Login");
            }

            User dbUser = db.Users
                .FirstOrDefault(u => u.Email == loginUser.LoginEmail);

            if (dbUser == null)
            {
                /* 
                In order to not reveal too much information (which one was wrong?)
                often generic messages are preferred, e.g., 'Incorrect credentials'.
                but for testing purposes we will be specific.
                */
                ModelState.AddModelError("LoginEmail", "Email not found.");
            }

            // If any error was added manually above.
            if (ModelState.IsValid == false)
            {
                return View("Login");
            }

            PasswordHasher<LoginUser> hasher = new PasswordHasher<LoginUser>();
            PasswordVerificationResult pwCompareResult = hasher.VerifyHashedPassword(loginUser, dbUser.Password, loginUser.LoginPassword);

            if (pwCompareResult == 0)
            {
                ModelState.AddModelError("LoginEmail", "Incorrect password.");
                return View("Login");
            }

            HttpContext.Session.SetInt32("UserId", dbUser.UserId);
            HttpContext.Session.SetString("FullName", dbUser.FullName());
            return RedirectToAction("Success");
        }

        [HttpPost("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
