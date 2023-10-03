using System;
using GetSomeInput;
using SendWithMailgun;

namespace Test
{
    class Program
    {
        static MailgunSender _Sender = null;
        static MailgunValidator _Validator = null;

        static string _Domain = null;
        static string _SendApiKey = null;
        static string _ValidateApiKey = null;

        static bool _RunForever = true;
        static SerializationHelper _Serializer = new SerializationHelper();

        static void Main(string[] args)
        {
            Console.WriteLine("");
            _Domain =         Inputty.GetString("Domain               :", null, false);
            _SendApiKey =     Inputty.GetString("API key (send)       :", null, false);
            _ValidateApiKey = Inputty.GetString("API key (validation) :", null, false);
            Console.WriteLine("");

            _Sender = new MailgunSender(_Domain, _SendApiKey);
            _Sender.Logger = Console.WriteLine;

            _Validator = new MailgunValidator(_ValidateApiKey);
            _Validator.Logger = Console.WriteLine;

            while (_RunForever)
            {
                string userInput = Inputty.GetString("Command [?/help]:", null, false);

                switch (userInput)
                {
                    case "q":
                        _RunForever = false;
                        break;
                    case "cls":
                        Console.Clear();
                        break;
                    case "?":
                        Menu();
                        break;
                    case "send":
                        SendEmail();
                        break;
                    case "validate":
                        ValidateEmail();
                        break;
                }
            }
        }

        static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("Available commands:");
            Console.WriteLine("   q           Quit this program");
            Console.WriteLine("   cls         Clear the screen");
            Console.WriteLine("   ?           Help, this menu");
            Console.WriteLine("   send        Send an email");
            Console.WriteLine("   validate    Validate an email address");
            Console.WriteLine("");
        }

        static void SendEmail()
        {
            string to =   Inputty.GetString("To      :", null, true);
            if (String.IsNullOrEmpty(to)) return;
            string from = Inputty.GetString("From    :", null, true);
            if (String.IsNullOrEmpty(to)) return;
            string cc =   Inputty.GetString("CC      :", null, true);
            string bcc =  Inputty.GetString("BCC     :", null, true);
            string subj = Inputty.GetString("Subject :", null, true);
            bool html =  Inputty.GetBoolean("HTML    :", false);
            string body = Inputty.GetString("Body    :", null, true);

            string id = _Sender.Send(to, from, subj, body, html, cc, bcc);
            Console.WriteLine("ID: " + id);
        }

        static void ValidateEmail()
        {
            string address = Inputty.GetString("Address :", null, true);
            if (String.IsNullOrEmpty(address)) return;

            MailgunValidationResult result = _Validator.Validate(address);
            Console.WriteLine(_Serializer.SerializeJson(result, true));
        }
    }
}
