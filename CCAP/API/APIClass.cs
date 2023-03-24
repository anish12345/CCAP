using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Specialized;
using System.Xml;
using System.Threading;

namespace CCAPP.Class
{

    public static class CreditCard
    {
        public static String Status = String.Empty;
        public static String CardHolder = String.Empty;
        public static String AuthorizationCode = String.Empty;
        public static String ApplicationExpiryDate = String.Empty;
        public static String AccountNumber = String.Empty;
        public static String TransactionType = String.Empty;
        public static String PaymentType = String.Empty;
        public static String EntryMode = String.Empty;
        public static String ResponseType = String.Empty;
        public static Decimal AmountApproved = 0;
        public static Decimal RequestedAmount = 0;
        public static String TokenNo = "";
        public static String CCRefNo = "";
        public static String ACHRefNo = "";
        public static String CHKNo = "";
        public static String CHKDt = "";
        public static String CardType = "0";

    }
    public static class APIClass
    {


        static FileStream _fs;
        static String sFileName = "__ApiSetting.dll";
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct APIPara
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string LogoURL;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string ConURL;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string RepURL;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string AuthName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string AuthSiteId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string AuthKey;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpNo;
            public short TerminalCon;
            public short SignatureCap;
        }

        public static Boolean APISET;
        public static String ConURL = String.Empty;
        public static String TrnURL = String.Empty;
        public static String RepURL = String.Empty;
        public static String LogoURL = String.Empty;
        public static String AuthName = String.Empty;
        public static String AuthKey = String.Empty;
        public static String AuthSiteId = String.Empty;
        public static String LocalIp = String.Empty;
        public static String Response = String.Empty;


        public static String LoginStatus = String.Empty;
        public static String LoginVaildKey = String.Empty;
        public static String LoginTransKey = String.Empty;
        public static String LoginErrMsg = "No Error";

        public static int APITerminal = 0;
        public static int APILineItemDisplay = 0;
        public static int APISignatureCapture = 0;
        public static int APIProcessCC = 0;

        public static void APIInit()
       {
            FromBinaryReaderBlock();
        }

        public static String RmvCur(String Curr)
        {
            return Curr.Replace("$", "").Replace("(", "").Replace(")", "").Replace(",", "");
        }
        static APIPara FromBinaryReaderBlock()
        {
            APIPara s = new APIPara();
            try
            {
                if (!File.Exists(Environment.CurrentDirectory + "\\" + sFileName))
                {
                    //MessageBox.Show("No Credit Card Settings found..!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    APISET = false;
                    return s;
                }
                FileInfo file = new FileInfo(Environment.CurrentDirectory + "\\" + sFileName);
                _fs = new FileStream(Environment.CurrentDirectory + "\\" + sFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                BinaryReader br = new BinaryReader(_fs);
                byte[] buff = br.ReadBytes(Marshal.SizeOf(typeof(APIPara)));
                GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
                //Marshal the bytes
                s = (APIPara)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(APIPara));
                APISET = true;
                LogoURL = s.LogoURL;
                ConURL =  s.ConURL;
                RepURL = s.RepURL;
                AuthName = s.AuthName;
                AuthSiteId =  s.AuthSiteId;
                AuthKey = s.AuthKey;
                APITerminal =  s.TerminalCon;
                APISignatureCapture = s.SignatureCap;
                LocalIp = s.IpNo;
                handle.Free();
                _fs.Close();
            }
            catch (Exception Exp)
            {
                //MessageBox.Show(Exp.Message, "Error in FromBinaryReaderBlock..", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return s;
        }
        public static string TerminalExcute(string URL, NameValueCollection QueryStringParameters = null, NameValueCollection RequestHeaders = null)
        {
            string ResponseText = null;
            using (WebClient client = new WebClient())
            {
                try
                {
                    if (RequestHeaders != null)
                    {
                        if (RequestHeaders.Count > 0)
                        {
                            foreach (string header in RequestHeaders.AllKeys)
                                client.Headers.Add(header, RequestHeaders[header]);
                        }
                    }
                    if (QueryStringParameters != null)
                    {
                        if (QueryStringParameters.Count > 0)
                        {
                            foreach (string parm in QueryStringParameters.AllKeys)
                                client.QueryString.Add(parm, QueryStringParameters[parm]);
                        }
                    }

                    byte[] ResponseBytes = client.DownloadData(URL);
                    ResponseText = Encoding.UTF8.GetString(ResponseBytes);
                }
                catch (WebException exception)
                {
                    ResponseText = exception.Message;
                }
            }
            return ResponseText;
        }

        public static Boolean AuthenticationExecute(String XMLReq, String ReqURL)
        {
            String Tmp = String.Empty;
            Boolean Result = false;
            HttpWebRequest request = null;
            XmlDocument soapEnvelopeXml = null;
            Stream stream = null;
            XmlDocument doc = null;
            WebResponse response = null;
            StreamReader rd = null;
            try
            {
                request = CreateWebRequest(ReqURL);
                soapEnvelopeXml = new XmlDocument();

                soapEnvelopeXml.LoadXml(XMLReq);
                Thread.Sleep(10);

                stream = request.GetRequestStream();
                Thread.Sleep(20);
                soapEnvelopeXml.Save(stream);

                response = request.GetResponse();
                Thread.Sleep(20);
                rd = new StreamReader(response.GetResponseStream());
                Thread.Sleep(20);
                string soapResult = rd.ReadToEnd();

                doc = new XmlDocument();
                doc.LoadXml(soapResult);
                string json = JsonConvert.SerializeXmlNode(doc);
                Response = json;
                Root _Root = new Root();
                _Root = JsonConvert.DeserializeObject<Root>(json);
                if (_Root.SoapEnvelope.SoapBody.CreateTransactionResponse.CreateTransactionResult.Messages == null)
                    LoginStatus = "Successfully Login";
                else
                    LoginStatus = _Root.SoapEnvelope.SoapBody.CreateTransactionResponse.CreateTransactionResult.Messages.ToString();
                if (_Root.SoapEnvelope.SoapBody.CreateTransactionResponse.CreateTransactionResult.ValidationKey == null)
                    LoginVaildKey = "N/a";
                else
                    LoginVaildKey = _Root.SoapEnvelope.SoapBody.CreateTransactionResponse.CreateTransactionResult.ValidationKey.ToString();

                if (_Root.SoapEnvelope.SoapBody.CreateTransactionResponse.CreateTransactionResult.TransportKey == null)
                    LoginTransKey = "N/a";
                else
                    LoginTransKey = _Root.SoapEnvelope.SoapBody.CreateTransactionResponse.CreateTransactionResult.TransportKey.ToString();
                Result = true;
            }
            catch (Exception Exp)
            {
                LoginErrMsg = Exp.Message;
            }
            return Result;
        }

        static HttpWebRequest CreateWebRequest(String URL)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(URL);
            webRequest.Headers.Add(@"SOAP:Action");
            webRequest.ContentType = " text/xml; charset=utf-8";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        //public static void NumPress(object sender, KeyPressEventArgs e)
        //{
        //    if (!char.IsControl(e.KeyChar) && (!char.IsDigit(e.KeyChar))
        // && (e.KeyChar != '.') && (e.KeyChar != '-'))
        //        e.Handled = true;

        //    // only allow one decimal point
        //    if (e.KeyChar == '.' && (sender as TextBox).Text.IndexOf('.') > -1)
        //        e.Handled = true;

        //    // only allow minus sign at the beginning
        //    if (e.KeyChar == '-' && (sender as TextBox).Text.Length > 0)
        //        e.Handled = true;
        //}
    }


    public class CreateTransactionResponse
    {
        [JsonProperty("@xmlns")]
        public string Xmlns { get; set; }
        public CreateTransactionResult CreateTransactionResult { get; set; }
    }

    public class CreateTransactionResult
    {
        public string TransportKey { get; set; }
        public string ValidationKey { get; set; }
        public object Messages { get; set; }
    }

    public class Root_1
    {
        public string Status { get; set; }
        public string ResponseMessage { get; set; }
        public AdditionalParameters AdditionalParameters { get; set; }
    }


    public class Root
    {
        [JsonProperty("?xml")]
        public Xml Xml { get; set; }

        [JsonProperty("soap:Envelope")]
        public SoapEnvelope SoapEnvelope { get; set; }
    }

    public class SoapBody
    {
        public CreateTransactionResponse CreateTransactionResponse { get; set; }
    }

    public class SoapEnvelope
    {
        [JsonProperty("@xmlns:soap")]
        public string XmlnsSoap { get; set; }

        [JsonProperty("@xmlns:xsi")]
        public string XmlnsXsi { get; set; }

        [JsonProperty("@xmlns:xsd")]
        public string XmlnsXsd { get; set; }

        [JsonProperty("soap:Body")]
        public SoapBody SoapBody { get; set; }
    }

    public class Xml
    {
        [JsonProperty("@version")]
        public string Version { get; set; }

        [JsonProperty("@encoding")]
        public string Encoding { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AdditionalParameters
    {
        public string SignatureData { get; set; }
        public AmountDetails AmountDetails { get; set; }
        public EMV EMV { get; set; }
    }

    public class Amount
    {
        public string AmountAuthorized { get; set; }
        public string AmountOther { get; set; }
    }

    public class AmountDetails
    {
        public string UserTip { get; set; }
        public string Cashback { get; set; }
        public string Donation { get; set; }
        public string Surcharge { get; set; }
        public Discount Discount { get; set; }
    }

    public class ApplicationCryptogram
    {
        public string CryptogramType { get; set; }
        public string Cryptogram { get; set; }
    }

    public class ApplicationInformation
    {
        public string Aid { get; set; }
        public string ApplicationLabel { get; set; }
        public string ApplicationExpiryDate { get; set; }
        public string ApplicationEffectiveDate { get; set; }
        public string ApplicationInterchangeProfile { get; set; }
        public string ApplicationVersionNumber { get; set; }
        public string ApplicationTransactionCounter { get; set; }
        public string ApplicationUsageControl { get; set; }
        public string ApplicationPreferredName { get; set; }
        public string ApplicationDisplayName { get; set; }
    }

    public class CardInformation
    {
        public string MaskedPan { get; set; }
        public string PanSequenceNumber { get; set; }
        public string CardExpiryDate { get; set; }
    }

    public class Discount
    {
        public string Total { get; set; }
    }

    public class EMV
    {
        public ApplicationInformation ApplicationInformation { get; set; }
        public CardInformation CardInformation { get; set; }
        public ApplicationCryptogram ApplicationCryptogram { get; set; }
        public string CvmResults { get; set; }
        public string IssuerApplicationData { get; set; }
        public string TerminalVerificationResults { get; set; }
        public string UnpredictableNumber { get; set; }
        public Amount Amount { get; set; }
        public string PosEntryMode { get; set; }
        public TerminalInformation TerminalInformation { get; set; }
        public TransactionInformation TransactionInformation { get; set; }
        public string CryptogramInformationData { get; set; }
        public string PINStatement { get; set; }
        public string CvmMethod { get; set; }
        public string IssuerActionCodeDefault { get; set; }
        public string IssuerActionCodeDenial { get; set; }
        public string IssuerActionCodeOnline { get; set; }
        public string AuthorizationResponseCode { get; set; }
    }

    public class Root_Trans
    {
        public string Status { get; set; }
        public string AmountApproved { get; set; }
        public string AuthorizationCode { get; set; }
        public string CardHolder { get; set; }
        public string Cardholder { get; set; }
        public string AccountNumber { get; set; }
        public string PaymentType { get; set; }
        public string EntryMode { get; set; }
        public string ErrorMessage { get; set; }
        public string Token { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string ResponseType { get; set; }
        public string ValidationKey { get; set; }
        public AdditionalParameters AdditionalParameters { get; set; }
        public string RFMIQ { get; set; }
    }

    public class CC_Detail
    {
        public string Status { get; set; }
        public string AuthorizationCode { get; set; }
        public string CardType { get; set; }
        public string CardHolder { get; set; }
        public string CardAdd { get; set; }
        public string CardNumber { get; set; }
        public string MaskCardNumber { get; set; }
        public string TransactionID { get; set; }
        public string ExpDt { get; set; }
        public string EntryMode { get; set; }

        public string Token { get; set; }
        public string AVS { get; set; }
        public string CVV { get; set; }
        public string ZipCode { get; set; }
        public string ValidationKey { get; set; }
    }

    public class TerminalInformation
    {
        public string TerminalType { get; set; }
        public string IfdSerialNumber { get; set; }
        public string TerminalCountryCode { get; set; }
        public string TerminalID { get; set; }
        public string TerminalActionCodeDefault { get; set; }
        public string TerminalActionCodeDenial { get; set; }
        public string TerminalActionCodeOnline { get; set; }
    }

    public class TransactionInformation
    {
        public string TransactionType { get; set; }
        public string TransactionCurrencyCode { get; set; }
        public string TransactionStatusInformation { get; set; }
    }


}
