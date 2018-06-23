using System;
using MinhBank.entity;
using MinhBank.model;
using MinhBank.utility;

namespace MinhBank.controller
{
    public class TransactionController
    {
        public AccountModel model = new AccountModel();

        public bool Withdraw()
        {
            Console.Clear();
            Console.Out.Flush();
            Console.WriteLine("> Vui lòng nhập số tiền bạn muốn rút:");
            var amount = Utility.GetUnsignDecimalNumber();
            if (amount > Program.currentLoggedIn.Balance)
            {
                return false;
            }

            var historyTransaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = Transaction.TransactionType.WITHDRAW,
                Amount = amount,
                Content = "Rút " + amount,
                SenderAccountNumber = Program.currentLoggedIn.AccountNumber,
                ReceiverAccountNumber = Program.currentLoggedIn.AccountNumber,
                Status = Transaction.ActiveStatus.DONE
            };
            if (model.UpdateBalance(Program.currentLoggedIn, historyTransaction))
            {
                Console.WriteLine("Transaction success!");
                return true;
            }

            Console.WriteLine("Transaction fails, please try again!");
            return false;
        }

        public void Deposit()
        {
            Console.Clear();
            Console.Out.Flush();
            Console.WriteLine("Deposit.");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Please enter amount to deposit: ");
            var amount = Utility.GetUnsignDecimalNumber();
            Console.WriteLine("Please enter message content: ");
            var content = Console.ReadLine();
//            Program.currentLoggedIn = model.GetAccountByUserName(Program.currentLoggedIn.Username);
            var historyTransaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = Transaction.TransactionType.DEPOSIT,
                Amount = amount,
                Content = content,
                SenderAccountNumber = Program.currentLoggedIn.AccountNumber,
                ReceiverAccountNumber = Program.currentLoggedIn.AccountNumber,
                Status = Transaction.ActiveStatus.DONE
            };
            if (model.UpdateBalance(Program.currentLoggedIn, historyTransaction))
            {
                Console.WriteLine("Transaction success!");
            }
            else
            {
                Console.WriteLine("Transaction fails, please try again!");
            }

            Program.currentLoggedIn = model.GetAccountByUserName(Program.currentLoggedIn.Username);
            Console.WriteLine("Current balance: " + Program.currentLoggedIn.Balance);
            Console.WriteLine("Press enter to continue!");
            Console.ReadLine();
        }

        public bool Transfer()
        {
            Console.Clear();
            Console.Out.Flush();
            Console.WriteLine("Vui lòng nhập chính xác tên tài khoản hoặc tên chủ khoản mà bạn muốn chuyển tới:");
            AccountController ac = new AccountController();
            var searchKey = Console.ReadLine().ToLower();
            Account receiveAccount = model.GetAccountByUserName(searchKey);
            if (receiveAccount == null)
            {
                Console.WriteLine("Không tìm thấy tài khoản mà bạn vừa nhập!");
                Console.WriteLine("Vui lòng thử lại...");
                Console.ReadLine();
                return false;
            }

            Console.WriteLine("> Vui lòng nhập số tiền bạn muốn gửi:");
            var amount = Utility.GetUnsignDecimalNumber();
            if (amount > Program.currentLoggedIn.Balance)
            {
                Console.WriteLine("Không thể chuyển số tiền lớn hơn số dư");
                return false;
            }

            Console.WriteLine("Nhập nội dung: ");
            string content = Console.ReadLine();
            var historyTransaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = Transaction.TransactionType.TRANSFER,
                Amount = amount,
                Content = content,
                SenderAccountNumber = Program.currentLoggedIn.AccountNumber,
                ReceiverAccountNumber = receiveAccount.AccountNumber,
                Status = Transaction.ActiveStatus.DONE
            };
            Console.WriteLine("Bạn có chắc muốn thực hiện giao dịch này không ? (y/n) ");
            string choice = Console.ReadLine();
            if (choice == "y")
            {
                if (model.UpDateBalanceTranfer(Program.currentLoggedIn, receiveAccount, historyTransaction))
                {
                    Console.WriteLine("Transaction success!");
                }
                else
                {
                    Console.WriteLine("Transaction fails, please try again!");
                }
            }
            else if (choice == "n")
            {
                Console.WriteLine("Đã hủy giao dịch");
                Console.ReadLine();
            }

            return true;
        }

        public void CheckBalance() // Dịch bởi Phúc.
        {
            Program.currentLoggedIn = model.GetAccountByUserName(Program.currentLoggedIn.Username);
            Console.WriteLine("Account Information");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Full name: " + Program.currentLoggedIn.FullName);
            Console.WriteLine("Account number: " + Program.currentLoggedIn.AccountNumber);
            Console.WriteLine("Balance: " + Program.currentLoggedIn.Balance);
            Console.WriteLine("Press enter to continue!");
            Console.ReadLine();
        }
    }
}