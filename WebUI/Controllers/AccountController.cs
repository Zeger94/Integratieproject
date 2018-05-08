﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using BL;
using Domain.Account;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebUI.Models;
using Microsoft.Owin.Security.DataProtection;
using System.Configuration;
using System.Web.Configuration;

namespace WebUI.Controllers
{
    [Authorize]
    [RequireHttps]
    public partial class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? makeSignInManager();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? makeUserManager();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ApplicationSignInManager makeSignInManager()
        {
            var uM = makeUserManager();
            return new ApplicationSignInManager(uM, HttpContext.GetOwinContext().Authentication);
        }

        public ApplicationUserManager makeUserManager()
        {
            var context = HttpContext.GetOwinContext().Get<ApplicationDbContext<ApplicationUser>>();
            var userstore = new ApplicationUserStore<ApplicationUser>(context) { TenantId = (int) System.Web.HttpContext.Current.Session["PlatformID"] };
            ApplicationUserManager uM = new ApplicationUserManager(userstore);
            return uM;
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public virtual ActionResult Login(string returnUrl)
        {
            var context = HttpContext.GetOwinContext().Get<ApplicationDbContext<ApplicationUser>>();
            var userstore = new ApplicationUserStore<ApplicationUser>(context) { TenantId = (int) System.Web.HttpContext.Current.Session["PlatformID"] };
            UserManager = new ApplicationUserManager(userstore);
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.platId = (int) System.Web.HttpContext.Current.Session["PlatformID"];
            return View();
        }

        [AllowAnonymous]
        public virtual ActionResult LoginNew(string returnUrl)
        {
            //var context = HttpContext.GetOwinContext().Get<ApplicationDbContext<ApplicationUser>>();
            //var userstore = new ApplicationUserStore<ApplicationUser>(context) { TenantId = id };
            //UserManager = new ApplicationUserManager(userstore);
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public async Task<bool> CheckIfAdminAsync(LoginViewModel model)
        {
            int oldplat = (int)System.Web.HttpContext.Current.Session["PlatformID"];

            System.Web.HttpContext.Current.Session["PlatformID"] = 0;
            var adminUM = makeUserManager();
            var user = await adminUM.FindByEmailAsync(model.Email);
            System.Web.HttpContext.Current.Session["PlatformID"] = oldplat;
            if (user != null)
            {
                return true;
            } else
            {
                return false;
            }
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            var result = SignInStatus.Failure;
            if (!ModelState.IsValid)
            {
                return RedirectToAction("LoginNew", model);
            }

            if (await CheckIfAdminAsync(model))
            {
             var currentPlatId = (int)System.Web.HttpContext.Current.Session["PlatformID"];
             System.Web.HttpContext.Current.Session["PlatformID"] = 0;
             var adminSM = makeSignInManager();
             result = await adminSM.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: true);
             System.Web.HttpContext.Current.Session["PlatformID"] = currentPlatId;
            } else
            {
                // Require the user to have a confirmed email before they can log on.
                var user = await UserManager.FindByNameAsync(model.Email);

                if (user != null)
                {
                    if (!await UserManager.IsEmailConfirmedAsync(user.Id))
                    {
                        string callbackUrl = await SendEmailConfirmationTokenAsync(user.Id, "Confirm your account-Resend");
                        ViewBag.errorMessage = "You must have a confirmed email to log on.";
                        return View("Error");
                    }
                }

                // In case of Admin trying to login
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, change to shouldLockout: true
                result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: true);

            }

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View("LoginNew", model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public virtual async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }
        [AllowAnonymous]
        public ActionResult MakePartial()
        {
            if (System.Web.HttpContext.Current.Session["PlatformID"] == null)
            {
                ViewBag.platId = 0;
            }
            else { 
                ViewBag.platId = (int)System.Web.HttpContext.Current.Session["PlatformID"];
            }
            return PartialView("_LoginPartial");
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public virtual ActionResult Register()
        {
            var context = HttpContext.GetOwinContext().Get<ApplicationDbContext<ApplicationUser>>();
            var userstore = new ApplicationUserStore<ApplicationUser>(context) { TenantId = (int)System.Web.HttpContext.Current.Session["PlatformID"] };
            UserManager = new ApplicationUserManager(userstore);
            return View();
        }

        [AllowAnonymous]
        public virtual ActionResult RegisterNew()
        {
            //var context = HttpContext.GetOwinContext().Get<ApplicationDbContext<ApplicationUser>>();
            //var userstore = new ApplicationUserStore<ApplicationUser>(context) { TenantId = id };
            //UserManager = new ApplicationUserManager(userstore);
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email , Email = model.Email, TenantId = (int)System.Web.HttpContext.Current.Session["PlatformID"] };
                var result = await UserManager.CreateAsync(user, model.Password);
                //CreateDomainUser(user.Id, user.Email, model.voornaam, model.achternaam, model.geboortedatum);
                if (result.Succeeded)
                {
                    //await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    string callbackUrl = await SendEmailConfirmationTokenAsync(user.Id, "Confirm your account");
                    //await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");


                    return View("PleaseConfirmEmail");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View("RegisterNew", model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public virtual async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [AllowAnonymous]
        public virtual async Task<ActionResult> ConfirmEmailNew(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmailNew" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [Authorize(Roles = "Admin")]
        public virtual ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email /* + PlatformController.currentPlatform */);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public virtual ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public virtual ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            
            var user = await UserManager.FindByNameAsync(model.Email /* + PlatformController.currentPlatform */);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public virtual ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        public virtual ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            Session["Workaround"] = 0;
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public virtual async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public virtual async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, TenantId = (int)System.Web.HttpContext.Current.Session["PlatformID"] };
                var result = await UserManager.CreateAsync(user);
                CreateDomainUser(user.Id, user.Email, "Joske", "Janssens", DateTime.Now);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        public virtual ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public virtual ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        public virtual ActionResult GenereerAlerts()
        {
            BL.AccountManager accountManager = new BL.AccountManager();
            accountManager.genereerAlerts();
            return new HttpStatusCodeResult(200);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        private async Task<string> SendEmailConfirmationTokenAsync(string userID, string subject)
        {
            string code = await UserManager.GenerateEmailConfirmationTokenAsync(userID);
            var callbackUrl = Url.Action("ConfirmEmailNew", "Account",
               new { userId = userID, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(userID, subject,
               "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

            return callbackUrl;
        }

        //gaat identity user linken aan een account uit ons domein.
        private void CreateDomainUser(string identityId, string email, string voornaam, string achternaam, DateTime geboorteDatum)
        {
            BL.AccountManager accountManager = new BL.AccountManager();
            Domain.Account.Account domainAccount = new Domain.Account.Account()
            {
                IdentityId = identityId,
                Email = email,
                Voornaam = voornaam,
                Achternaam = achternaam,
                GeboorteDatum = geboorteDatum,
                Dashboard = new Dashboard()
            };
            domainAccount.Dashboard.Configuratie = new Domain.Account.DashboardConfiguratie();
            accountManager.addUser(domainAccount);
        }
        #endregion

        public ActionResult IndexUsers()
        {
            IAccountManager accountManager = new AccountManager();
            List<Account> accounts = accountManager.GetAccounts();
            return View(accounts);
        }

        //Aanmaken van een user door admin
        public ActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]

        public async Task<ActionResult> CreateUser(RegisterViewModel model)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            await UserManager.CreateAsync(user, model.Password);
            CreateDomainUser(user.Id, user.Email, model.voornaam, model.achternaam, model.geboortedatum);
            return RedirectToAction("IndexUsers");
        }

        public ActionResult DeleteUser(string id)
        {
            IAccountManager accountManager = new AccountManager();
            accountManager.DeleteUser(id);
            return RedirectToAction("IndexUsers");
        }


        public ActionResult EditUser(string id)
        {
            IAccountManager accountManager = new AccountManager();
            return View(accountManager.getAccount(id));
        }

        [HttpPost]
        public ActionResult EditUser(Account account)
        {
           
            IAccountManager accountManager = new AccountManager();
            accountManager.UpdateUser(account);
            return RedirectToAction("IndexUsers");
        }

    }



}