using System;
using System.Globalization;
using MinhBank.controller;
using MinhBank.entity;
using MinhBank.utility;

namespace MinhBank.view
{
    public class MainView
    {
        private static AccountController _accountController = new AccountController();
        private static TransactionController _transactionController = new TransactionController();

        public static void GenerateMenu()
        {
            while (true)
            {
                if (Program.currentLoggedIn == null)
                {
                    GenerateGeneralMenu();
                }
                else
                {
                    GenerateCustomerMenu();
                }
            }
        }

        private static void GenerateCustomerMenu()
        {
            while (true)
            {
                Console.WriteLine("---------Minh_Bank---------");
                Console.WriteLine("Welcome back: " + Program.currentLoggedIn.FullName);
                Console.WriteLine("1. Kiểm tra số dư.");
                Console.WriteLine("2. Rút tiền.");
                Console.WriteLine("3. Gửi tiền.");
                Console.WriteLine("4. Chuyển tiền.");
                Console.WriteLine("5. Kiểm tra lịch sử giao dịch.");
                Console.WriteLine("6. Thoát.");
                Console.WriteLine("---------------------------------------------");
                Console.WriteLine("Nhập lựa chọn của bạn (1|2|3|4|5|6): ");
                var choice = Utility.GetInt32Number();
                switch (choice)
                {
                    case 1:
                        _transactionController.CheckBalance();
                        break;
                    case 2:
                        if (_transactionController.Withdraw())
                        {
                            Console.WriteLine("Rút tiền thành công.");
                            Console.WriteLine("Bấm Enter để tiếp tục...");
                            Console.ReadLine();
                        }
                        else
                        {
                            Console.WriteLine("Rút tiền thất bại.");
                            Console.WriteLine("Bấm Enter để tiếp tục...");
                            Console.ReadLine();
                        }
                        break;
                    case 3:
                        _transactionController.Deposit();
                        break;
                    case 4:
                        _transactionController.Transfer();
                        break;
                    case 5 :
                        GenerateTransactionHistoryMenu();
                        break;
                    case 6:
                        Console.WriteLine("See you later.");
                        Environment.Exit(1);
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
        }

        public static void GenerateTransactionHistoryMenu()
        {
            var _success = true;
            // Console.WriteLine("Vui lòng chọn một trong các kiểu kiểm tra lịch sử mà bạn muốn phía dưới:");
            Console.WriteLine("Vui lòng chọn một trong các kiểu kiểm tra lịch sử mà bạn muốn phía dưới:");
            while (true)
            {
                Console.Clear();
                Console.Out.Flush();
                Console.WriteLine("========== TRANSACTION HISTORY MENU ==========");
                Console.WriteLine("1. Lịch sử 5 ngày gần nhất");
                Console.WriteLine("2. Lịch sử 10 giao dịch gần nhất");
                Console.WriteLine("3. Lịch sử theo khoảng thời gian (ngày/tháng/năm ~ ngày/tháng/năm)");
                Console.WriteLine("4. Lịch sử giao dịch giữa bạn với một ai đó");
                Console.WriteLine("5. Quay trở về");
                Console.WriteLine("==============================================");

                var choice = Utility.GetUnsignDecimalNumber();
                if (choice > 0 && choice < 5)
                {
                    if (_accountController.GetTransactionHistory(choice))
                    {
                        Console.WriteLine("Kiểm tra lịch sử thành công!");
                    }
                    else _success = false;
                }
                else if (choice == 5) _success = false;
                else Console.WriteLine("Bạn đã nhập lựa chọn không phù hợp! Vui lòng chọn lại..");


                if ((Program.currentLoggedIn != null) || !_success)
                {

                    break;
                }
                else
                {
                    Console.WriteLine("\tBấm ENTER để quay lại...");
                    Console.ReadLine();
                    // break;
                }
            }
        }

        private static void GenerateGeneralMenu()
        {
            while (true)
            {
                Console.WriteLine("---------WELCOME TO MINH_BANK---------");
                Console.WriteLine("1. Đăng ký.");
                Console.WriteLine("2. Đăng nhập.");
                Console.WriteLine("3. Thoát.");
                Console.WriteLine("---------------------------------------------");
                Console.WriteLine("Nhập lựa chọn của bạn (1|2|3): ");                
                var choice = Utility.GetInt32Number();
                switch (choice)
                {
                    case 1:
                        _accountController.Register();
                        break;
                    case 2:
                        if (_accountController.DoLogin())
                        {
                            Console.WriteLine("Login success.");
                        }
                        break;
                    case 3:
                        Console.WriteLine("See you later.");
                        Environment.Exit(1);
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }

                if (Program.currentLoggedIn != null)
                {
                    break;
                }
            }
        }
        
    }
}