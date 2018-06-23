using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MinhBank.entity;
using MinhBank.model;
using MinhBank.utility;
using MinhBank.view;

namespace MinhBank
{
    class Program
    {
        public static Account currentLoggedIn;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            MainView.GenerateMenu();
        }
    }
}