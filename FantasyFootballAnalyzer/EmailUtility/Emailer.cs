using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EmailUtility
{
    public class Emailer
    {
        //sends an email to the address specified within the method, via google's SMTP service
        //you must have a valid google account, with an app password generated for this purpose
        public static void SendEmail(string subject, string body)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("gmailaddress");

            // The important part -- configuring the SMTP client
            SmtpClient smtp = new SmtpClient();
            smtp.Port = 587; // [1] You can try with 465 also, I always used 587 and got success
            smtp.EnableSsl = true;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network; // [2] Added this
            smtp.UseDefaultCredentials = false; // [3] Changed this
            smtp.Credentials = new NetworkCredential("gmailaddress", "customAppCode");
                // [4] Added this. Note, first parameter is NOT string.
            smtp.Host = "smtp.gmail.com";

            //recipient address
            mail.To.Add(new MailAddress("recipientAddress"));

            mail.Body = body;
            mail.Subject = subject;
            smtp.Send(mail);
        }
    }
}
