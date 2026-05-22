using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

using IO.Swagger.Client;
using IO.Swagger.Api;
using IO.Swagger.Model;
using OAuthDemo.Models;

namespace OAuthDemo.Controllers
{
    [Authorize]
    public class DemoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DemoController()
        {
            _context = new ApplicationDbContext();
        }

        // Helper method to verify ownership
        private Demo GetUserDemo(string id)
        {
            var userId = User.Identity.GetUserId();
            return _context.Demos.SingleOrDefault(d => d.Id == id && d.UserId == userId);
        }

        // GET: Demo
        public ActionResult Index()
        {
            Demo DemoModel = new Demo(Guid.NewGuid().ToString());
            DemoModel.UserId = User.Identity.GetUserId();
            _context.Demos.Add(DemoModel);
            _context.SaveChanges();
            return View(DemoModel);
        }

        public ActionResult RegisterApplication()
        {
            return null;
        }

        // step 2
        public ActionResult RedirectMerchant(RedirectMerchantInput input)
        {
            System.Diagnostics.Debug.WriteLine(_context.Demos.ToString());
            var SavedModel = GetUserDemo(input.Id);
            if (SavedModel == null)
                return new HttpUnauthorizedResult();
            
            SavedModel.ClientId = input.ClientId;
            SavedModel.RedirectUri = input.RedirectUri;
            SavedModel.Read = input.Read;
            SavedModel.Write = input.Write;
            SavedModel.State = input.State;
            SavedModel.Sub = input.Sub;
            SavedModel.updateRedirectMerchantUrl();

            _context.SaveChanges();
            return Redirect(SavedModel.RedirectMerchantUrl);
        }

        // step 3
        public ActionResult RetrieveAccessToken(RetrieveAccessTokenInput input)
        {
            var SavedModel = GetUserDemo(input.Id);
            if (SavedModel == null)
                return new HttpUnauthorizedResult();
            
            SavedModel.GrantType = input.GrantType;
            SavedModel.Code = input.Code;
            SavedModel.ClientId = input.ClientId;
            // WARNING: Demo application only - credentials stored in plaintext for educational purposes
            // PRODUCTION CODE MUST encrypt credentials at rest using ASP.NET Data Protection API or column-level encryption
            SavedModel.ClientSecret = input.ClientSecret;

            try
            {
                RetrievingRefreshingApi instance = new RetrievingRefreshingApi();
                var response = instance.GetToken(grantType: SavedModel.GrantType, clientId: SavedModel.ClientId, code: SavedModel.Code, clientSecret: SavedModel.ClientSecret, platform: SavedModel.platform);
                SavedModel.Step3Response = response.ToJson();
                SavedModel.AccessToken = response.AccessToken;
                SavedModel.RefreshToken = response.RefreshToken;
            }
            catch
            {
                SavedModel.Step3Response = Demo.RetrieveErrorResponse;
            }

            _context.SaveChanges();
            return View("Index", SavedModel);
        }

        // step 4
        public ActionResult ChargeCreditCard(ChargeCreditCardInput input)
        {
            var SavedModel = GetUserDemo(input.Id);
            if (SavedModel == null)
                return new HttpUnauthorizedResult();
            
            SavedModel.AccessToken = input.AccessToken;
            SavedModel.CardNumber = input.CardNumber;
            SavedModel.ExpirationDate = input.ExpirationDate;
            SavedModel.Amount = input.Amount;

            try
            {
                SavedModel.Step4Response = net.authorize.sample.ChargeCreditCard.Run(
                    SavedModel.AccessToken, 
                    SavedModel.CardNumber, 
                    SavedModel.ExpirationDate, 
                    SavedModel.Amount);
            }
            catch
            {
                SavedModel.Step4Response = Demo.APICallErrorResponse;
            }

            _context.SaveChanges();
            return View("Index", SavedModel);
        }

        public ActionResult GetTransactionDetails(GetTransactionDetailsInput input)
        {
            var SavedModel = GetUserDemo(input.Id);
            if (SavedModel == null)
                return new HttpUnauthorizedResult();
            
            SavedModel.AccessToken = input.AccessToken;
            SavedModel.TransactionId = input.TransactionId;

            try
            {
                SavedModel.Step4Response = net.authorize.sample.GetTransactionDetails.Run(
                    SavedModel.AccessToken,
                    SavedModel.TransactionId
                    );
            }
            catch
            {
                SavedModel.Step4Response = Demo.APICallErrorResponse;
            }

            _context.SaveChanges();
            return View("Index", SavedModel);
        }

        // step 5
        public ActionResult RefreshAccessToken(RefreshAccessTokenInput input)
        {
            var SavedModel = GetUserDemo(input.Id);
            if (SavedModel == null)
                return new HttpUnauthorizedResult();
            
            SavedModel.ClientId = input.ClientId;
            SavedModel.ClientSecret = input.ClientSecret;
            SavedModel.GrantType = input.GrantType;
            SavedModel.RefreshToken = input.RefreshToken;

            try
            {
                RetrievingRefreshingApi instance = new RetrievingRefreshingApi();
                var response = instance.GetToken(grantType: SavedModel.GrantType, clientId: SavedModel.ClientId, clientSecret: SavedModel.ClientSecret, refreshToken: SavedModel.RefreshToken);
                SavedModel.Step5Response = response.ToJson();
            }
            catch
            {
                SavedModel.Step5Response = Demo.RefreshErrorResponse;
            }

            _context.SaveChanges();
            return View("Index", SavedModel);
        }

        public ActionResult RedirectRevokePermissions()
        {
            return Redirect(Demo.RevokePermissionsUrl);
        }
    }
}
