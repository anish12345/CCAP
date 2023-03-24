using CCAP.Models;
using CCAPP.Class;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CCAP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public HomeController(ILogger<HomeController> logger, IHttpContextAccessor contxt)
        {
            _logger = logger;
            _contextAccessor = contxt;
        }

        public decimal txtDueAmt = 0;
        public int CashierId = 1;
        public string OrderNumber = "";
        public int RegisterNumber = 1;
        public int Mode = 0;
        public decimal TaxAmt = 0;
        public decimal DueAmt = 0;
        public bool APIReq = false;
        public CancellationTokenSource tokenSource = null;

        public CC_Detail CC_Det { get; set; }
        public int ManualCC { get; set; }

        StreamWriter StrWrLog = null;


        public IActionResult Index()
        {

            APIClass.APIInit();

            HttpContext.Session.SetString("LogoURL", APIClass.LogoURL);
            HttpContext.Session.SetString("ConURL", APIClass.ConURL);
            HttpContext.Session.SetString("RepURL", APIClass.RepURL);
            HttpContext.Session.SetString("AuthName", APIClass.AuthName);
            HttpContext.Session.SetString("AuthSiteId", APIClass.AuthSiteId);
            HttpContext.Session.SetString("AuthKey", APIClass.AuthKey);
            HttpContext.Session.SetInt32("APITerminal", Convert.ToInt16(APIClass.APITerminal));
            HttpContext.Session.SetInt32("APISignatureCapture", Convert.ToInt16(APIClass.APISignatureCapture));
            HttpContext.Session.SetString("LocalIp", Convert.ToString(APIClass.LocalIp));
            HttpContext.Session.SetInt32("APISET", Convert.ToInt16(APIClass.APISET));


            //    if (APIClass.APISET)

            //    btnOk.Enabled = true;
            //else
            //    btnOk.Enabled = false;
            // LogFileCreate();

            return View();
        }

        //void LogFileCreate()
        //{
        //    StreamWriter StrWrLog = null;
        //    String sFile = String.Format("{0}{1:MM-dd-yyyy}{2}", "CC-LOG-", DateTime.Now, ".LOG");
        //    String sPath = Environment.CurrentDirectory + "\\CCLog-Files";
        //    if (!System.IO.Directory.Exists(sPath))
        //        System.IO.Directory.CreateDirectory(sPath);
        //    StrWrLog = new StreamWriter(sPath + "\\" + sFile, true);
        //    StrWrLog.WriteLine(String.Format("{0:MM-dd-yyyy hh:mm:ss tt}{1}", DateTime.Now, ",OPEN.."));

        //}

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public void Process(decimal DueAmt, string OrderNumbers)
        {
            txtDueAmt = DueAmt;
            OrderNumber = OrderNumbers;
            APIGetTransportKey(txtDueAmt);
            Thread.Sleep(1000);
            APIConnect();

        }



        #region Manual Entry
        [HttpGet]
        public async Task<JsonResult>   ManualEntry()
        {
            //http://[CED-HOSTNAME]:8080/v1/pos?Action=Cancel&Format=XML
            String Result = String.Empty;
            String Param = "Action=InitiateKeyedEntry";
            Param += "&Format=JSON";
            String URL = @"http://" + APIClass.LocalIp + ":8080/v2/pos?" + Param;
            var t = Task.Run(() => { Result = APIClass.TerminalExcute(URL); });
            await t;
            if (Result.Contains("Success"))
            {
                  Result = "Enter Credit Card Number Manualy on terminal";
                  ViewBag.rtfLoginRes = Result;
            }
            else
            {
                ViewBag.rtfLoginRes = Result;
            }

            return Json(Result);
        }

        #endregion


        #region API Connect
        public void APIConnect()
        {
            //Successfully Login
            //rtfLoginRes.Text = "";
            if (APIClass.LoginStatus == "Successfully Login")
            {
                //rtfLoginRes.BackColor = Color.FromArgb(128, 255, 128);
                //rtfLoginRes.ForeColor = Color.Black;
                ViewBag.TransportText = "Transport Key Successfull";
                //btnManualPOS.Enabled = true;
                //btnManualEntry.Enabled = true;
                //btnTrnCancel.Enabled = true;
                //btnNewTran.Visible = false;

            }
            else
            {
                //rtfLoginRes.B
            }
            APIReq = true;
            ViewBag.rtfLoginRes = "Auth Rquest Sent\r";
            //rtfLoginRes.AppendText("Auth Rquest Sent\r");
            if (APIClass.APITerminal == 1)
            {
                TransactionResp();
            }

        }
        #endregion


        #region API Transaction Response
        async void TransactionResp()
        {

            //btnTrnCancel.Visible = true;
            //btnManualEntry.Visible = true;
            //txtStatus.Text = String.Empty;
            String ExpDt = String.Empty;
            String Result = String.Empty;
            String Param = "TransportKey=" + APIClass.LoginTransKey;
            Param += "&Format=JSON";
            String URL = @"http://" + APIClass.LocalIp + ":8080/v2/pos?" + Param;
            var t = Task.Run(() => { Result = APIClass.TerminalExcute(URL); });
            await t;
            // rtfLoginRes.AppendText(Result + "\r");
            if (Result.Contains("timed out"))
            {

                {
                    //  APIGetScreenId("02");
                    // MessageBox.Show("Timed out !, Please try again!...");

                    //  rtfLoginRes.Text = Result;
                    Thread.Sleep(100);
                }

                return;
            }

            Root_Trans _Root = new Root_Trans();
            try
            {
                //   rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.Text = ""));
                _Root = JsonConvert.DeserializeObject<Root_Trans>(Result);
                if (_Root != null)
                {
                    // txtStatus.Text = _Root.Status.ToUpper();
                    if (_Root.Status.ToUpper() == "APPROVED")
                    {

                        // btnNewTran.Visible = true;
                        //btnTrnCancel.Visible = false;
                        //btnManualPOS.Enabled = false;
                        //btnManualEntry.Enabled = false;

                        Param = "Status: " + _Root.Status.ToUpper() + "\n";
                        Param += "Card Holder: " + _Root.Cardholder + "\n";
                        if (_Root.AdditionalParameters.EMV != null)
                        {
                            Param += "Auth Code: " + _Root.AuthorizationCode + "  | Exp.Date: " + _Root.AdditionalParameters.EMV.ApplicationInformation.ApplicationExpiryDate + "\n";
                        }
                        else
                        {
                            Param += "Auth Code: " + _Root.AuthorizationCode + "  | Exp.Date: \n";
                        }
                        Param += "CC Last 4: " + _Root.AccountNumber.Replace("*", "") + "  | PMT TYPE: " + _Root.PaymentType + "\n";
                        Param += "Trans Type: " + _Root.TransactionType + "   | Entry: " + _Root.EntryMode + "\n";
                        Param += "Approved Amount:  " + Decimal.Parse(_Root.AmountApproved).ToString("C");


                        CreditCard.Status = _Root.Status;
                        CreditCard.CardHolder = _Root.Cardholder;
                        CreditCard.AuthorizationCode = _Root.AuthorizationCode;
                        CreditCard.ApplicationExpiryDate = _Root.AdditionalParameters.EMV != null ? _Root.AdditionalParameters.EMV.ApplicationInformation.ApplicationExpiryDate : "";
                        CreditCard.AccountNumber = _Root.AccountNumber.Replace("*", "");
                        CreditCard.TransactionType = _Root.TransactionType == "SALE" ? "1" : "2";
                        CreditCard.ResponseType = _Root.ResponseType;

                        CreditCard.AmountApproved = Decimal.Parse(_Root.AmountApproved);
                        CreditCard.TokenNo = _Root.Token;
                        String DataLine = String.Empty;
                        DataLine = String.Format("{0:MM-dd-yyyy hh:mm:ss tt}", DateTime.Now) + ",";
                        DataLine += "TERMINAL,";
                        DataLine += OrderNumber + ",";
                        DataLine += CreditCard.Status + ",";
                        DataLine += CreditCard.CardHolder + ",";
                        DataLine += CreditCard.TransactionType + ",";
                        DataLine += CreditCard.AuthorizationCode + ",";
                        DataLine += CreditCard.ResponseType + ",";
                        DataLine += CreditCard.TokenNo + ",";
                        DataLine += _Root.TransactionType + ",";
                        DataLine += CreditCard.AmountApproved;

                        StrWrLog.WriteLine(DataLine);
                        //StrWrLog.Flush();
                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.Text = Param));
                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.BackColor = Color.LimeGreen));
                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.ForeColor = Color.Black));
                        // MessageBox.Show(rtfLoginRes.Text);
                        //  btnNewTran.Focus();
                        APIReq = true;
                        Mode = 1;

                    }
                    else if (_Root.Status.ToUpper() == "USERCANCELLED")
                    {
                        //btnManualEntry.BeginInvoke((Action)(() => btnManualEntry.Enabled = false));

                        //APIReq = false;
                        //Param = "Error: " + _Root.ErrorMessage + "\rStatus: " + _Root.Status;
                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.Text = Param));

                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.BackColor = Color.Red));
                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.ForeColor = Color.White));

                    }
                    else
                    {
                        //btnManualEntry.BeginInvoke((Action)(() => btnManualEntry.Enabled = false));
                        //APIReq = false;
                        //Param = "Error: " + _Root.ErrorMessage + "\rStatus: " + _Root.Status;

                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.Text = Param));
                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.BackColor = Color.Red));
                        //rtfLoginRes.BeginInvoke((Action)(() => rtfLoginRes.ForeColor = Color.White));
                    }
                }
            }
            catch (Exception Exp)
            {
                // MessageBox.Show(Exp.Message, "Error in CancelTerminal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
        async void APIGetTransportKey(decimal txtDueAmt)
        {

            Boolean Result;
            String TrnType = String.Empty;
            TrnType = txtDueAmt < 0 ? "_REFUND" : "SALE";
            var t = Task.Run(() => { Result = APIClass.AuthenticationExecute(XMLCon(TrnType), APIClass.ConURL); });
            await t;

        }


        #region XML Envelope 
        String XMLCon(String TrnType = "SALE")
        {
            //OrderNumber	String	1-8	The order or invoice number associated with the transaction.
            Decimal Amt = Math.Abs(txtDueAmt);

            String XMLReq = @"<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
                      <s:Header/>
                      <s:Body>
                        <CreateTransaction xmlns:i='http://www.w3.org/2001/XMLSchema-instance' xmlns='http://transport.merchantware.net/v4/'>";
            XMLReq += "<merchantName>" + APIClass.AuthName + "</merchantName>";
            XMLReq += "<merchantSiteId>" + APIClass.AuthSiteId + "</merchantSiteId>";
            XMLReq += "<merchantKey>" + APIClass.AuthKey + "</merchantKey>";
            XMLReq += "<request>";
            XMLReq += "<TransactionType>" + TrnType + "</TransactionType > ";
            XMLReq += "<Amount>" + Amt.ToString() + "</Amount>";
            XMLReq += "<ClerkId>" + CashierId + "</ClerkId>";
            XMLReq += "<OrderNumber>" + OrderNumber + "</OrderNumber>";
            XMLReq += "<Dba>POS Demo</Dba>";
            XMLReq += "<SoftwareName>Zayne Solutions</SoftwareName>";
            XMLReq += "<SoftwareVersion>1.1.1.97</SoftwareVersion>";

            // XMLReq += "<AddressLine1></AddressLine1>";
            // XMLReq += "<Zip></Zip>";
            // XMLReq += "<Cardholder></Cardholder>";
            XMLReq += "<LogoLocation>" + APIClass.LogoURL + "</LogoLocation>";
            XMLReq += "<TransactionId>1</TransactionId>";
            XMLReq += "<ForceDuplicate>true</ForceDuplicate>";
            // XMLReq += "<CustomerCode></CustomerCode>";
            // XMLReq += "<PoNumber></PoNumber>";
            XMLReq += "<TaxAmount>" + TaxAmt + "</TaxAmount>";
            XMLReq += "<EntryMode>Undefined</EntryMode>";
            XMLReq += "<TerminalId>" + RegisterNumber + "</TerminalId>";
            XMLReq += " <EnablePartialAuthorization>false</EnablePartialAuthorization>";
            XMLReq += "</request>";
            XMLReq += "</CreateTransaction>";
            XMLReq += "</s:Body>";
            XMLReq += "</s:Envelope>";
            return XMLReq;
        }
        #endregion
        public IActionResult Exit()
        {
            //write your logic here to save the file on a disc            
            return Json("1");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        private void btnManualPOS_Click(object sender, EventArgs e)
        {
            ShowManualCCPanel();
            if (APIClass.APITerminal == 1)
                CancelTerminal();
        }



        #region Show Manual Credit Card Panel
        void ShowManualCCPanel()
        {
            Decimal Amount = DueAmt;
           // this.Size = new Size(572, 869);
           // this.CenterToScreen();
          //  txtManualTenderCCAmt.Text = Amount.ToString("C");


           // rtfManual.Text = "https://transport.merchantware.net/v4/transportweb.aspx?transportKey=" + APIClass.LoginTransKey;
           // txtURL.Text = rtfManual.Text;
           // webCC.Url = new System.Uri(rtfManual.Text, System.UriKind.Absolute);

           // pnlManual.Visible = true;
            //pnlManual.BringToFront();
        }
        #endregion

        #region Cancel Terminal 
        async void CancelTerminal()
        {
            APIReq = false;
            //http://[CED-HOSTNAME]:8080/v1/pos?Action=Cancel&Format=XML

            String Result = String.Empty;
            String Param = "Action=Cancel";
            Param += "&Format=JSON";
            String URL = @"http://" + APIClass.LocalIp + ":8080/v2/pos?" + Param;
            var t = Task.Run(() => { Result = APIClass.TerminalExcute(URL); });
            await t;
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }

            Root_1 _Root = new Root_1();
            try
            {
                _Root = JsonConvert.DeserializeObject<Root_1>(Result);
                if (_Root != null)
                {
                   // rtfLoginRes.Text = _Root.Status.ToString();
                }
                Thread.Sleep(1000);

            }
            catch (Exception Exp)
            {
              //  MessageBox.Show(Exp.Message, "Error in CancelTerminal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion


        //private void webCC_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        //{
        //    String DataLine = String.Empty;
        //    if (e.Url != webCC.Url)
        //        return;
        //    txtURL2.Text = e.Url.ToString();  // New Link
        //    if (txtURL.Text != txtURL2.Text && txtURL2.TextLength > 105) // need to check for change or url
        //    {
        //        rtfManual.Text = webCC.Url.ToString();
        //        rtfManual.BringToFront();
        //        CC_Det = ManualCCDataParse();

        //        if (CC_Det.Status.ToUpper() == "APPROVED")
        //        {
        //            btnManualOK.Enabled = true;
        //            btnManualCancel.Enabled = false;


        //        }
        //        else
        //        {
        //            btnManualOK.Enabled = false;
        //            btnManualCancel.Enabled = true;
        //        }
        //    }
        //}

        #region Manual Credit Card Data Parse
        CC_Detail ManualCCDataParse()
        {
            int Idx = 0;
            CC_Detail _Root = new CC_Detail();
            String Data = String.Empty;
            String[] Tmp;
            String[] Tmp1;
            //Data = "https://transport.merchantware.net/v4/Processed.aspx?Status=APPROVED&RefID=4764956167&Token=4764956167&AuthCode=OK9999&CardType=3&CardNumber=************4111&ExpDate=1225&AVS=empty&CVV=empty&EntryMode=1&ValidationKey=137af1bc-9bcf-4ae9-a6c8-09da51f8a1d1&Cardholder=r&Address=r&ZipCode=11418&MaskedCardNumber=541333******4111&JWT=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlOWQ2NDU1Mi1hZjQ5LTQxZDUtODA3MC1hZTY4MjRiYWY2ZmIiLCJuYmYiOjE2NzE0NTk1NjgsImV4cCI6MTY3MTQ2MDc2OCwiaWF0IjoxNjcxNDU5NTY4LCJpc3MiOiJUcmFuc3BvcnRXZWIiLCJhdWQiOiJUcmFuc3BvcnRXZWIifQ.akM_iDngvzus_LLaMdnILZdNAucYdX5WcB95DsUjMTg&a687=281";
            //Data = "https://transport.merchantware.net/v4/Processed.aspx?Status=APPROVED&RefID=4778880987&Token=4778880987&AuthCode=OK9999&CardType=3&CardNumber=************4111&ExpDate=1225&AVS=empty&CVV=empty&EntryMode=1&TransactionID=ABC123&ValidationKey=8bb2c0e1-d5ee-42e4-8a43-8b3de8e43a04&Cardholder=Visa+Test+Card&Address=123+Main+Street&ZipCode=02110&MaskedCardNumber=541333******4111&JWT=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkZjY0MDVlNS1iMGU3LTQyMWMtODA4ZS1jMDMyNDcwMTYzZDgiLCJuYmYiOjE2NzIwMzYwODksImV4cCI6MTY3MjAzNzI4OSwiaWF0IjoxNjcyMDM2MDg5LCJpc3MiOiJUcmFuc3BvcnRXZWIiLCJhdWQiOiJUcmFuc3BvcnRXZWIifQ.m-f0fBJ6gg2Ap-oCwJoEG8HbIcZ2I4l5LDc7hmC4MlI&a362=756";
           // Data = rtfManual.Text;
            Data = Data.Replace("&", "|");
            Idx = Data.IndexOf("?");
            Data = Data.Substring(Idx + 1, Data.Length - Idx - 1);

            if (Data.Contains("USER_CANCELLED"))
                goto EndLvl;

            Tmp = Data.Split('|');

            for (int i = 0; i < Tmp.Length; i++)
            {
                Tmp1 = Tmp[i].Split('=');
                if (Tmp1[0] == "Status")
                    _Root.Status = Tmp1[1];
                else if (Tmp1[0] == "Token")
                    _Root.Token = Tmp1[1];
                else if (Tmp1[0] == "AuthCode")
                    _Root.AuthorizationCode = Tmp1[1];
                else if (Tmp1[0] == "CardType")
                    _Root.CardType = Tmp1[1];
                else if (Tmp1[0] == "CardNumber")
                    _Root.CardNumber = Tmp1[1];
                else if (Tmp1[0] == "ExpDate")
                    _Root.ExpDt = "20" + Tmp1[1].Substring(2, 2) + "-" + Tmp1[1].Substring(0, 2) + "-01"; // YYYY-MM-dd
                else if (Tmp1[0] == "AVS")
                    _Root.AVS = Tmp1[1];
                else if (Tmp1[0] == "CVV")
                    _Root.CVV = Tmp1[1];
                else if (Tmp1[0] == "EntryMode")
                    _Root.EntryMode = Tmp1[1];
                else if (Tmp1[0] == "TransactionID")
                    _Root.TransactionID = Tmp1[1];
                else if (Tmp1[0] == "ValidationKey")
                    _Root.ValidationKey = Tmp1[1];
                else if (Tmp1[0] == "Cardholder")
                    _Root.CardHolder = Tmp1[1];
                else if (Tmp1[0] == "Address")
                    _Root.CardAdd = Tmp1[1];
                else if (Tmp1[0] == "ZipCode")
                    _Root.ZipCode = Tmp1[1];
                else if (Tmp1[0] == "MaskedCardNumber")
                    _Root.MaskCardNumber = Tmp1[1];
            }
        EndLvl:
            return _Root;
        }
        #endregion


        public void AddNewTransaction(object sender, EventArgs e)
        {
            OrderNumber = String.Empty;
            DueAmt = 0;
            txtDueAmt = 0;

        }

    }
}