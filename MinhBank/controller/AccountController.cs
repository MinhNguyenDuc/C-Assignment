using System;
using System.Collections.Generic;
using MinhBank.entity;
using MinhBank.model;
using MinhBank.utility;

namespace MinhBank.controller
{
    public class AccountController
    {
        private AccountModel model = new AccountModel();

        public void Register()
        {
            Console.WriteLine("Please enter account information");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Username: ");
            var username = Console.ReadLine();
            Console.WriteLine("Password: ");
            var password = Console.ReadLine();
            Console.WriteLine("Confirm Password: ");
            var cpassword = Console.ReadLine();
            Console.WriteLine("Identity Card: ");
            var identityCard = Console.ReadLine();
            Console.WriteLine("Full Name: ");
            var fullName = Console.ReadLine();
            Console.WriteLine("Email: ");
            var email = Console.ReadLine();
            Console.WriteLine("Phone: ");
            var phone = Console.ReadLine();
            var account = new Account(username, password, cpassword, identityCard, phone, email, fullName);
            var errors = account.CheckValid();
            if (errors.Count == 0)
            {
                model.Save(account);
                Console.WriteLine("Register success!");
                Console.ReadLine();
            }
            else
            {
                Console.Error.WriteLine("Please fix following errors and try again.");
                foreach (var messagErrorsValue in errors.Values)
                {
                    Console.Error.WriteLine(messagErrorsValue);
                }

                Console.ReadLine();
            }
        }

        public Boolean DoLogin()
        {
            // Lấy thông tin đăng nhập phía người dùng.
            Console.WriteLine("Please enter account information");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Username: ");
            var username = Console.ReadLine();
            Console.WriteLine("Password: ");
            var password = Console.ReadLine();
            var account = new Account(username, password);
            // Tiến hành validate thông tin đăng nhập. Kiểm tra username, password khác null và length lớn hơn 0.
            var errors = account.ValidLoginInformation();
            if (errors.Count > 0)
            {
                Console.WriteLine("Invalid login information. Please fix errors below.");
                foreach (var messagErrorsValue in errors.Values)
                {
                    Console.Error.WriteLine(messagErrorsValue);
                }

                Console.ReadLine();
                return false;
            }

            account = model.GetAccountByUserName(username);
            if (account == null)
            {
                // Sai thông tin username, trả về thông báo lỗi không cụ thể.
                Console.WriteLine("Invalid login information. Please try again 1.");
                return false;
            }

            // Băm password người dùng nhập vào kèm muối và so sánh với password đã mã hoá ở trong database.
            if (account.Password != Hash.GenerateSaltedSHA1(password, account.Salt))
            {
                // Sai thông tin password, trả về thông báo lỗi không cụ thể.
                Console.WriteLine("Invalid login information. Please try again 2.");
                return false;
            }

            // Login thành công. Lưu thông tin đăng nhập ra biến static trong lớp Program.
            Program.currentLoggedIn = account;
            return true;
        }

        public bool GetTransactionHistory(decimal choice)
        {
            List<Transaction> lT = null;
            switch (choice)
            {
                case 1:
                {
                    // Lịch sử 5 ngày gần nhất
                    lT = model.GetTransactionHistory5Days(Program.currentLoggedIn.AccountNumber);
                    break;
                }
                case 2:
                {
                    // Lịch sử 10 giao dịch gần nhất
                    lT = model.GetTransactionHistory10Newest(Program.currentLoggedIn.AccountNumber);
                    break;
                }
                case 3:
                {
                    Console.WriteLine("Chức năng đang phát triển");
                    break;
                }
            }

            if (lT != null)
            {
                // var count = 0;
                if (lT.Count > 0)
                {
                    Console.Out.Flush();
                    Console.Clear();

                    Console.WriteLine("> Tìm thấy {0} kết quả tương ứng.", lT.Count);
                    Console.WriteLine(String.Format("|{0,30}|{1,15}|{2,15}|{3,20}|{4,15}|{5,15}|", "Thời gian",
                        "Mã giao dịch", "Người gửi", "Nội dung", "Số tiền", "Người nhận"));
                    Console.WriteLine(String.Format("|{0,30}|{1,15}|{2,15}|{3,20}|{4,15}|{5,15}|",
                        "------------------------------", "---------------", "----------", "--------------------",
                        "---------------", "----------"));
                    foreach (var t in lT)
                    {
                        Console.WriteLine("|{0,30}|{1,15}|{2,15}|{3,20}|{4,15}|{5,15}|", t.CreatedAt,
                            t.Id, t.SenderAccountNumber,
                            t.Type != Transaction.TransactionType.TRANSFER
                                ? t.Type == Transaction.TransactionType.DEPOSIT ? "đã gửi" : "đã rút"
                                : "đã chuyển khoản", t.Amount.ToString("N0"),
                            t.Type == Transaction.TransactionType.TRANSFER
                                ? "" + t.ReceiverAccountNumber
                                : "");
                    }

                    Console.WriteLine(String.Format("|{0,30}|{1,15}|{2,10}|{3,20}|{4,15}|{5,10}|",
                        "------------------------------", "---------------", "----------", "--------------------",
                        "---------------", "----------"));
                }
                else Console.WriteLine("Không tìm thấy giao dịch phù hợp với điều kiện tương ứng!");
            }
            else return false;

            if (lT.Count > 0) return true;
            return false;
        }
    }
}