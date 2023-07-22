using System;
using SendWithMailgun;

namespace Test
{
    class Program
    {
        static MailgunSender _Sender = null;

        static void Main(string[] args)
        {
            _Sender = new MailgunSender(
                InputString("Domain  :", null, false),
                InputString("API key :", null, false));
            _Sender.Logger = Console.WriteLine;

            while (true)
            {
                string id = _Sender.Send(
                    InputString("To      :", null, false),
                    InputString("From    :", null, false),
                    InputString("Subject :", null, false),
                    InputString("Body    :", null, false));
            }
        }

        private static string InputString(string question, string defaultAnswer, bool allowNull)
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }

    }
}
