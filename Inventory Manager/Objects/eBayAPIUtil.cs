using System;
using eBay.Service.Call;
using eBay.Service.Core.Soap;
using eBay.Service.Core.Sdk;

namespace Inventory_Manager.Objects
{
    public static class eBayAPIUtil
    {
        public static GeteBayDetailsCall GetDetails(ApiContext context)
        {
            if (App.eBayDetails == null) {
                App.eBayDetails = new GeteBayDetailsCall(context);
                App.eBayDetails.GeteBayDetails(null);
                App.eBayShippingMethods = new System.Collections.Generic.List<eBayShipping>();

                foreach (ShippingServiceDetailsType sm in App.eBayDetails.ShippingServiceDetailList)
                {
                    if (sm.ValidForSellingFlow == false || sm.InternationalService == true)
                        continue;

                    foreach (ShippingCategoryDetailsType cat in App.eBayDetails.ShippingCategoryDetailList)
                    {
                        if (cat.ShippingCategory == sm.ShippingCategory) {
                            App.eBayShippingMethods.Add(new eBayShipping(sm, cat));
                            break;
                        }
                    }
                }
            }

            return App.eBayDetails;
        }

        public static ApiContext CreateContext(string version, SiteCodeType siteCode, string ruName, string developer, string application, string certificate)
        {
            var myContext = new ApiContext() {
                RuName = ruName,
                Version = version,
                ApiCredential = new ApiCredential() {
                    eBayToken = null,
                    ApiAccount = new ApiAccount() {
                        Developer = developer,
                        Application = application,
                        Certificate = certificate
                    },
                }
            };


            if (myContext.ApiCredential.ApiAccount.Application.Contains("-SBX-") == true) {
                myContext.SoapApiServerUrl = "https://api.sandbox.ebay.com/wsapi";
                myContext.EPSServerUrl = "https://api.sandbox.ebay.com/ws/api.dll";
                myContext.SignInUrl = "https://signin.sandbox.ebay.com/ws/eBayISAPI.dll?SignIn";
            }
            else {
                myContext.SoapApiServerUrl = "https://api.ebay.com/wsapi";
                myContext.EPSServerUrl = "https://api.ebay.com/ws/api.dll";
                myContext.SignInUrl = "https://ebay.com/ws/eBayISAPI.dll?SignIn";
            }

            return myContext;
        }

        public static void AddLogger(ApiContext context, eBay.Service.Util.FileLogger logger) {
            if (context.ApiLogManager != null) {
                context.ApiLogManager = new ApiLogManager();
            }

            context.ApiLogManager.EnableLogging = true;
            context.ApiLogManager.ApiLoggerList.Add(logger);
        }

        public static Boolean? RequestSignIn(ApiContext context) {
            String session = new GetSessionIDCall(context).GetSessionID(context.RuName);
            
            var result = new Windows.Window_eBayAuth() {
                Homepage = $"{context.SignInUrl}&runame={System.Web.HttpUtility.UrlEncode(context.RuName)}&SessID={System.Web.HttpUtility.UrlEncode(session)}"
            }.ShowDialog();

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
            if (result == true) {
                context.ApiCredential.eBayToken = new FetchTokenCall(context).FetchToken(session);
            }
            else {
                context.ApiCredential.eBayToken = null;
            }
            
            context.ApiCredential.eBayToken = (result == true ? new FetchTokenCall(context).FetchToken(session) : null);
            return result;
        }

        public static Boolean GetAuthorization(ApiContext context, String authFile = null)
        {
            context.ApiCredential.eBayToken = null;

            if (authFile != null && System.IO.File.Exists(authFile)) {
                context.ApiCredential.eBayToken = System.IO.File.ReadAllText(authFile);

                TokenStatusCodeType status = GetTokenStatus(context).Status;
                // new RevokeTokenCall().RevokeToken(false);
                if (status != TokenStatusCodeType.Active) {
                    if (status == TokenStatusCodeType.Expired) {
                        Adericium.DialogBox.Show("The authorization for this application has expired. You will be asked to sign in to renew this application's authorization.");
                    }
                    else {
                        Adericium.DialogBox.Show("The authorization for this application is invalid. You will need to sign in to reauthorize this application.");
                    }

                    System.IO.File.Delete(authFile);
                    context.ApiCredential.eBayToken = null;
                }
                else {
                    return true;
                }
            }

            if (RequestSignIn(context) == true)  {
                if (GetTokenStatus(context).Status == TokenStatusCodeType.Active) {
                    if (authFile != null) {
                        System.IO.File.WriteAllText(authFile, context.ApiCredential.eBayToken);
                    }

                    return true;
                }
                else
                {
                    Adericium.DialogBox.Show("The authorization for this application is invalid.");
                }
            }

            return false;
        }

        public static TokenStatusType GetTokenStatus(ApiContext context)
        {
            return new GetTokenStatusCall(context).GetTokenStatus();
        }
    }
}
