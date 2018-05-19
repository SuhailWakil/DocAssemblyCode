using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web.Script.Serialization;
using System.Security;
using System.Security.Permissions;

namespace SDCounty.ReportingServices.AccelaDocumentsService
{
    public class AccelaDocument
    {
        public static void Main()
        {
            //Byte[] ret = GetInspectionSignature("5696562", "inspector", "http://10.47.41.52:3080", "wsuser", "wsuser");

            List<Byte[]> retAll = GetInspectionImages("5842332", "http://10.47.41.52:3080", "wsuser", "wsuser");
            //List<Byte[]> retAll = GetInspectionImages("5840735", "http://10.47.41.52:3080", "wsuser", "wsuser");
            

            Console.WriteLine("HERE SWAKIL2: " + retAll);
        }

        public static Byte[] GetInspectionSignature(string inspectionid, string type, string bizServer, string userId, string password)
        {
            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(new WebPermission(PermissionState.Unrestricted));
            ps.AddPermission(new WebPermission(NetworkAccess.Accept, bizServer));
            ps.AddPermission(new WebPermission(NetworkAccess.Connect, bizServer));
            ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode | SecurityPermissionFlag.Execution));
            ps.Assert();

            var token = Authenticate(inspectionid, bizServer, userId, password);
            var docId = "";
            
            if (type.Equals("contractor"))
            {
                docId = GetInspectionContractorSign(bizServer, inspectionid, token);
            }
            else if (type.Equals("inspector"))
            {
                docId = GetInspectionInspectorSign(bizServer, inspectionid, token);
            }

            var byteArr = GetBytesProxy(bizServer, token, docId, 9999);
            return byteArr;
        }

        public static List<Byte[]> GetInspectionImages(string inspectionid, string bizServer, string userId, string password)
        {
            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(new WebPermission(PermissionState.Unrestricted));
            ps.AddPermission(new WebPermission(NetworkAccess.Accept, bizServer));
            ps.AddPermission(new WebPermission(NetworkAccess.Connect, bizServer));
            ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode | SecurityPermissionFlag.Execution));
            ps.Assert();

            var token = Authenticate(inspectionid, bizServer, userId, password);
                       
            List < Documents > resultList = GetInspectionDocuments(bizServer, inspectionid, token);
            List<Byte[]> imgByteArr = new List<Byte[]>();
            if (resultList != null)
            {
                foreach (var doc in resultList)
                {
                    if (!(doc.uploadedBy).Equals("EDMS")) { //SHOULD REMOVE WHEN EDMS BDOCUMENT ISSUE IS FIXED
                        var byteArr = GetBytesProxy(bizServer, token, doc.id.ToString(), 9999);
                        imgByteArr.Add(byteArr);
                    }
                }
            }
            //var jsonString = new JavaScriptSerializer().Serialize(imgByteArr);
            //Console.WriteLine(jsonString);

            return imgByteArr;
        }

        public static string Authenticate(string inspectionid, string bizServer, string userId, string password)
        {
            WebClient client = new WebClient();
            client.Headers.Add("content-type", "application/x-www-form-urlencoded");

            Dictionary<string, string> paramAuth =
            new Dictionary<string, string>();
            paramAuth.Add("agency", "COSD");
            paramAuth.Add("userId", userId);
            paramAuth.Add("password", password);
            JavaScriptSerializer serializer = new JavaScriptSerializer(); 
            string jsonParamAuth = serializer.Serialize(paramAuth);
            String ret = Encoding.ASCII.GetString(client.UploadData(bizServer + "/apis/agency/auth/", "POST", Encoding.Default.GetBytes(jsonParamAuth)));
            var retObj = serializer.Deserialize<AuthResponse>(ret);
            var token = retObj.result;
            return token;
        }

        public static string GetInspectionContractorSign(string bizServer, string inspId, string token)
        {
            List<Documents> resultList = GetInspectionDocuments(bizServer, inspId, token);

            foreach (var doc in resultList)
            {
                if (doc.fileName.Contains("Contractor"))
                {
                    return doc.id.ToString();
                }
            }
            return "";
        }

        public static string GetInspectionInspectorSign(string bizServer, string inspId, string token)
        {
            List<Documents> resultList = GetInspectionDocuments(bizServer, inspId, token);
            foreach (var doc in resultList)
            {
                if (doc.fileName.Contains("Inspector"))
                {
                    return doc.id.ToString();
                }
            }
            return "";
        }

        public static List<Documents> GetInspectionDocuments(string bizServer, string inspId, string token)
        {
            var url = $"/apis/v4/inspections/{inspId}/documents?token={token}";
            WebClient client = new WebClient();
            String ret = client.DownloadString(bizServer + url);
            
            var serializer = new JavaScriptSerializer();
            var retObj = serializer.Deserialize<InspDocuments>(ret);
            List<Documents> resultList = retObj.result;
            if (resultList != null) {
                resultList = resultList.OrderByDescending(o => o.uploadedDate).ToList();
            }
            return resultList;
        }

        public static byte[] GetBytesProxy(string bizServer, string token, string documentId, long length)
        {
            var url = $"/apis/v4/documents/{documentId}/download?token={token}";
            url = bizServer + url;
            byte[] buffer;
            Encoding encoding = Encoding.UTF8;
            Uri requestUri = new Uri(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.AllowAutoRedirect = false;
            request.Timeout = 0x4c4b40;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), encoding);
            using (MemoryStream stream2 = new MemoryStream())
            {
                int count = 0;
                byte[] buffer2 = new byte[length];
                while ((count = reader.BaseStream.Read(buffer2, 0, buffer2.Length)) > 0)
                {
                    stream2.Write(buffer2, 0, count);
                }
                buffer = stream2.ToArray();
            }
            reader.Close();
            return buffer;
        }
    }

    public class InspDocuments
    {
        public List<Documents> result { set; get; }
        public string status { set; get; }
    }

    public class Documents
    {
        public StatusObj status { get; set; }
        public string source { get; set; }
        public string uploadedDate { get; set; }
        public string uploadedBy { get; set; }
        public string department { get; set; }
        public string modifiedBy { get; set; }
        public string entityId { get; set; }
        public int id { get; set; }
        public string entityType { get; set; }
        public string fileName { get; set; }
        public string size { get; set; }
        public string modifiedDate { get; set; }
        public string serviceProviderCode { get; set; }
        public string statusDate { get; set; }
    }

    public class StatusObj
    {
        public string value { get; set; }
        public string text { get; set; }
    }

    public class AuthResponse
    {
        public ResponseObject responseStatus { set; get; }
        public string result { set; get; }
    }

    public class ResponseObject
    {
        public string status { get; set; }

    }
}
