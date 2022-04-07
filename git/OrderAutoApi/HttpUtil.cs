using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Deduce.Common.Utility
{
    /// <summary>
    /// 通用的请求工具
    /// </summary>
    public static class HttpUtil
    {
        /// <summary>
        /// 发起请求
        /// </summary>
        /// <param name="item">请求选项</param>
        /// <returns></returns>
        public static HttpResults Post(HttpItems item)
        {
            HttpWebRequest request = null;
            HttpResults result = new HttpResults();
            try
            {
                SetHttpItem(item);
                request = CreateRequest(item);
                result = GetResult(request, item);
            }
            catch (Exception e)
            {
                result = new HttpResults()
                {
                    Exception = e
                };
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
                if (result.Document == null)
                {
                    result.Document = string.Empty;
                }
            }
            return result;
        }
        /// <summary>
        /// 初始化请求选项
        /// </summary>
        /// <param name="item"></param>
        private static void SetHttpItem(HttpItems item)
        {
            if (string.IsNullOrEmpty(item.URL))
            {
                throw new ArgumentNullException("Url");
            }
            if (string.IsNullOrEmpty(item.Method))
            {
                item.Method = "GET";
            }
            if (item.Timeout == 0)
            {
                item.Timeout = 1000 * 125;
            }
            if (item.DataEncoding == null)
            {
                item.DataEncoding = Encoding.UTF8;
            }
        }
        /// <summary>
        /// 创建请求
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static HttpWebRequest CreateRequest(HttpItems item)
        {
            ServicePointManager.DefaultConnectionLimit = 200;
            if (item.URL.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) => true);
            }
            var request = (HttpWebRequest)WebRequest.Create(item.URL);
            if (item.X509Certificate != null)
            {
                request.ClientCertificates.Add(item.X509Certificate);
            }
            if (!string.IsNullOrEmpty(item.ContentType))
            {
                request.ContentType = item.ContentType;
            }
            if (!string.IsNullOrEmpty(item.Accept))
            {
                request.Accept = item.Accept;
            }
            if (!string.IsNullOrEmpty(item.Connection))
            {
                request.Connection = item.Connection;
            }
            if (!string.IsNullOrEmpty(item.Cookie))
            {
                request.Headers[HttpRequestHeader.Cookie] = item.Cookie;
            }
            if (!string.IsNullOrEmpty(item.Host))
            {
                request.Host = item.Host;
            }
            if (!string.IsNullOrEmpty(item.Referer))
            {
                request.Referer = item.Referer;
            }
            if (item.Timeout > 0)
            {
                request.Timeout = item.Timeout;
            }
            if (!string.IsNullOrEmpty(item.UserAgent))
            {
                request.UserAgent = item.UserAgent;
            }
            if (item.Credentials != null)
            {
                request.Credentials = item.Credentials;
            }
            if (!string.IsNullOrEmpty(item.Method))
            {
                request.Method = item.Method;
            }
            if (item.Data != null)
            {
                if (item.Data is string)
                {
                    using (var stream = request.GetRequestStream())
                    {
                        var bytes = item.DataEncoding.GetBytes(item.Data as string);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                if (item.Data is FormData)
                {
                    var data = item.Data as FormData;
                    using (var stream = request.GetRequestStream())
                    {
                        //if (data.Pairs.Any(a => a.Value is FormData.Meta))
                        //{
                            string boundary = "---------------------------" + Guid.NewGuid().GetHashCode();
                            request.ContentType = "multipart/form-data; boundary=" + boundary;
                            byte[] boundaryBytes = item.DataEncoding.GetBytes("\r\n--" + boundary + "\r\n");
                            foreach (var pair in data.Pairs)
                            {
                                if (pair.Value is string)
                                {
                                    stream.Write(boundaryBytes, 0, boundaryBytes.Length);
                                    string str = string.Format("Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}", pair.Key, pair.Value);
                                    byte[] bytes = item.DataEncoding.GetBytes(str);
                                    stream.Write(bytes, 0, bytes.Length);
                                }
                                else if (pair.Value is FormData.Meta)
                                {
                                    var value = pair.Value as FormData.Meta;
                                    stream.Write(boundaryBytes, 0, boundaryBytes.Length);
                                    string description = string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n",
                                    pair.Key, value.FileName, value.ContentType);
                                    byte[] fileHeader = item.DataEncoding.GetBytes(description);
                                    stream.Write(fileHeader, 0, fileHeader.Length);
                                    stream.Write(value.Data, 0, value.Data.Length);
                                }
                            }
                            var endBoundaryBytes = item.DataEncoding.GetBytes("\r\n--" + boundary + "--\r\n");
                            stream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
                       // }
                        //else
                        //{
                        //    var param = string.Join("&", data.Pairs.Select(s => string.Format("{0}={1}", s.Key, s.Value)));
                        //    var paramBytes = item.DataEncoding.GetBytes(param);
                        //    stream.Write(paramBytes, 0, paramBytes.Length);
                        //}
                    }
                }
            }
            else
            {
                request.ContentLength = 0;
            }
            request.KeepAlive = item.KeepAlive;
            request.AllowAutoRedirect = item.AllowAutoRedirect;
            return request;
        }
        /// <summary>
        /// 获取响应结果
        /// </summary>
        /// <param name="request"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static HttpResults GetResult(HttpWebRequest request, HttpItems item)
        {
            var result = new HttpResults();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                result.StatusCode = response.StatusCode;
                result.Headers = response.Headers;
                result.StatusDescription = response.StatusDescription;
                result.ContentType = response.ContentType;
                result.CharacterSet = response.CharacterSet;
                if (response.Cookies != null)
                {
                    result.Cookies = string.Join(";", response.Cookies.Cast<Cookie>().Select(s => string.Format("{0}={1}", s.Name, s.Value)));
                }
                using (var stream = response.GetResponseStream())
                {
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms, 1024 * 10);
                        var bytes = ms.ToArray();
                        result.Binary = bytes;
                        if (item.DocumentEncoding != null)
                        {
                            result.Document = item.DocumentEncoding.GetString(bytes);
                        }
                        else if (!string.IsNullOrEmpty(response.CharacterSet) && !"ISO-8859-1".Equals(response.CharacterSet, StringComparison.CurrentCultureIgnoreCase))
                        {
                            try
                            {
                                result.Document = Encoding.GetEncoding(response.CharacterSet).GetString(bytes);
                            }
                            catch
                            {
                                result.Document = string.Empty;
                            }
                        }
                        else
                        {
                            result.Document = Encoding.UTF8.GetString(bytes);
                        }

                    }
                }
            }
            return result;
        }
    }
    /// <summary>
    /// 获取响应结果
    /// </summary>
    public class HttpResults
    {
        /// <summary>
        /// 响应文档
        /// </summary>
        public string Document { get; set; }
        /// <summary>
        /// 响应字节
        /// </summary>
        public byte[] Binary { get; set; }
        /// <summary>
        /// 异常
        /// </summary>
        public Exception Exception { get; set; }
        /// <summary>
        /// 状态码
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }
        /// <summary>
        /// Cookie
        /// </summary>
        public string Cookies { get; set; }
        /// <summary>
        /// 获取来自服务器的与此响应关联的标头。
        /// </summary>
        public WebHeaderCollection Headers { get; set; }
        /// <summary>
        /// 状态描述
        /// </summary>
        public string StatusDescription { get; set; }
        /// <summary>
        /// 字符集
        /// </summary>
        public string CharacterSet { get; set; }
        /// <summary>
        /// 响应上下文类型
        /// </summary>
        public string ContentType { get; set; }
    }
    /// <summary>
    /// 请求选项
    /// </summary>
    public class HttpItems
    {
        /// <summary>
        /// Url:不能为空
        /// </summary>
        public string URL { get; set; }
        /// <summary>
        /// POST/GET
        /// Default:GET
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 请求参数
        /// Type:string/FormData
        /// </summary>
        public object Data { get; set; }
        /// <summary>
        /// 请求参数字符编码
        /// Default:UTF-8
        /// </summary>
        public Encoding DataEncoding { get; set; }
        /// <summary>
        /// 响应文档字符编码
        /// </summary>
        public Encoding DocumentEncoding { get; set; }
        /// <summary>
        /// 获取或设置 Accept HTTP 标头的值。
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// 请求上下文类型
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// 获取或设置 Connection HTTP 标头的值。
        /// </summary>
        public string Connection { get; set; }
        /// <summary>
        /// 请求Cookie
        /// </summary>
        public string Cookie { get; set; }
        /// <summary>
        /// 获取或设置要在 HTTP 请求中独立于请求 URI 使用的 Host 标头值。
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// 获取或设置一个值，该值指示是否与 Internet 资源建立持续型连接。
        /// </summary>
        public bool KeepAlive { get; set; }
        /// <summary>
        /// 获取或设置 Referer HTTP 标头的值。
        /// </summary>
        public string Referer { get; set; }
        /// <summary>
        /// 获取或设置 System.Net.HttpWebRequest.GetResponse 和 System.Net.HttpWebRequest.GetRequestStream方法的超时值（以毫秒为单位）。
        /// </summary>
        public int Timeout { get; set; }
        /// <summary>
        /// 获取或设置 User-agent HTTP 标头的值。
        /// </summary>
        public string UserAgent { get; internal set; }
        /// <summary>
        /// 获取或设置一个值，该值指示请求是否应跟随重定向响应。
        /// </summary>
        public bool AllowAutoRedirect { get; internal set; }
        /// <summary>
        /// 获取或设置请求的身份验证信息。
        /// </summary>
        public ICredentials Credentials { get; internal set; }
        /// <summary>
        /// 获取或设置与此请求关联的安全证书
        /// </summary>
        public X509Certificate X509Certificate { get; internal set; }
    }
    /// <summary>
    /// 表单参数
    /// </summary>
    public class FormData
    {
        public Dictionary<string, object> Pairs = new Dictionary<string, object>();
        public string ContentType { get; set; }
        public void Append(string name, object value)
        {
            Pairs.Add(name, value);
        }
        public void Append(string name, byte[] data, string filename = "", string contentType = "")
        {
            Pairs.Add(name, new Meta()
            {
                FileName = filename,
                ContentType = contentType,
                Data = data
            });
        }
        public class Meta
        {
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public byte[] Data { get; set; }
        }
    }
}
