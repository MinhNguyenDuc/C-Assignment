using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using MySql.Data.MySqlClient;
using MinhBank.entity;
using MinhBank.error;
using MinhBank.utility;

namespace MinhBank.model
{
    public class AccountModel
    {
        public Boolean Save(Account account)
        {
            DbConnection.Instance().OpenConnection(); // đảm bảo rằng đã kết nối đến db thành công.
            var salt = Hash.RandomString(7); // sinh ra chuỗi muối random.
            account.Salt = salt; // đưa muối vào thuộc tính của account để lưu vào database.
            // mã hoá password của người dùng kèm theo muối, set thuộc tính password mới.
            account.Password = Hash.GenerateSaltedSHA1(account.Password, account.Salt);
            var sqlQuery = "insert into `accounts` " +
                           "(`username`, `password`, `accountNumber`, `identityCard`, `balance`, `phone`, `email`, `fullName`, `salt`, `status`) values" +
                           "(@username, @password, @accountNumber, @identityCard, @balance, @phone, @email, @fullName, @salt, @status)";
            var cmd = new MySqlCommand(sqlQuery, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@username", account.Username);
            cmd.Parameters.AddWithValue("@password", account.Password);
            cmd.Parameters.AddWithValue("@accountNumber", account.AccountNumber);
            cmd.Parameters.AddWithValue("@identityCard", account.IdentityCard);
            cmd.Parameters.AddWithValue("@balance", account.Balance);
            cmd.Parameters.AddWithValue("@phone", account.Phone);
            cmd.Parameters.AddWithValue("@email", account.Email);
            cmd.Parameters.AddWithValue("@fullName", account.FullName);
            cmd.Parameters.AddWithValue("@salt", account.Salt);
            cmd.Parameters.AddWithValue("@status", account.Status);
            var result = cmd.ExecuteNonQuery();
            DbConnection.Instance().CloseConnection();
            return result == 1;
        }

        public bool UpdateBalance(Account account, Transaction historyTransaction)
        {
            DbConnection.Instance().OpenConnection(); // đảm bảo rằng đã kết nối đến db thành công.
            var transaction = DbConnection.Instance().Connection.BeginTransaction(); // Khởi tạo transaction.

            try
            {
                /**
                 * 1. Lấy thông tin số dư mới nhất của tài khoản.
                 * 2. Kiểm tra kiểu transaction. Chỉ chấp nhận deposit và withdraw.
                 *     2.1. Kiểm tra số tiền rút nếu kiểu transaction là withdraw.                 
                 * 3. Update số dư vào tài khoản.
                 *     3.1. Tính toán lại số tiền trong tài khoản.
                 *     3.2. Update số tiền vào database.
                 * 4. Lưu thông tin transaction vào bảng transaction.
                 */

                // 1. Lấy thông tin số dư mới nhất của tài khoản.
                var queryBalance = "select balance from `accounts` where username = @username and status = @status";
                MySqlCommand queryBalanceCommand = new MySqlCommand(queryBalance, DbConnection.Instance().Connection);
                queryBalanceCommand.Parameters.AddWithValue("@username", account.Username);
                queryBalanceCommand.Parameters.AddWithValue("@status", account.Status);
                var balanceReader = queryBalanceCommand.ExecuteReader();
                // Không tìm thấy tài khoản tương ứng, throw lỗi.
                if (!balanceReader.Read())
                {
                    // Không tồn tại bản ghi tương ứng, lập tức rollback transaction, trả về false.
                    // Hàm dừng tại đây.
                    throw new SpringHeroTransactionException("Invalid username");
                }

                // Đảm bảo sẽ có bản ghi.
                var currentBalance = balanceReader.GetDecimal("balance");
                balanceReader.Close();

                // 2. Kiểm tra kiểu transaction. Chỉ chấp nhận deposit và withdraw. 
                if (historyTransaction.Type != Transaction.TransactionType.DEPOSIT
                    && historyTransaction.Type != Transaction.TransactionType.WITHDRAW)
                {
                    throw new SpringHeroTransactionException("Invalid transaction type!");
                }

                // 2.1. Kiểm tra số tiền rút nếu kiểu transaction là withdraw.
                if (historyTransaction.Type == Transaction.TransactionType.WITHDRAW &&
                    historyTransaction.Amount > currentBalance)
                {
                    throw new SpringHeroTransactionException("Not enough money!");
                }

                // 3. Update số dư vào tài khoản.
                // 3.1. Tính toán lại số tiền trong tài khoản.
                if (historyTransaction.Type == Transaction.TransactionType.DEPOSIT)
                {
                    currentBalance += historyTransaction.Amount;
                }
                else if (historyTransaction.Type == Transaction.TransactionType.WITHDRAW)
                {
                    currentBalance -= historyTransaction.Amount;
                }

                // 3.2. Update số dư vào database.
                var updateAccountResult = 0;
                var queryUpdateAccountBalance =
                    "update `accounts` set balance = @balance where username = @username and status = 1";
                var cmdUpdateAccountBalance =
                    new MySqlCommand(queryUpdateAccountBalance, DbConnection.Instance().Connection);
                cmdUpdateAccountBalance.Parameters.AddWithValue("@username", account.Username);
                cmdUpdateAccountBalance.Parameters.AddWithValue("@balance", currentBalance);
                updateAccountResult = cmdUpdateAccountBalance.ExecuteNonQuery();

                // 4. Lưu thông tin transaction vào bảng transaction.
                var insertTransactionResult = 0;
                var queryInsertTransaction = "insert into `transactions` " +
                                             "(id, type, amount, content, senderAccountNumber, receiverAccountNumber, status) " +
                                             "values (@id, @type, @amount, @content, @senderAccountNumber, @receiverAccountNumber, @status)";
                var cmdInsertTransaction =
                    new MySqlCommand(queryInsertTransaction, DbConnection.Instance().Connection);
                cmdInsertTransaction.Parameters.AddWithValue("@id", historyTransaction.Id);
                cmdInsertTransaction.Parameters.AddWithValue("@type", historyTransaction.Type);
                cmdInsertTransaction.Parameters.AddWithValue("@amount", historyTransaction.Amount);
                cmdInsertTransaction.Parameters.AddWithValue("@content", historyTransaction.Content);
                cmdInsertTransaction.Parameters.AddWithValue("@senderAccountNumber",
                    historyTransaction.SenderAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@receiverAccountNumber",
                    historyTransaction.ReceiverAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@status", historyTransaction.Status);
                insertTransactionResult = cmdInsertTransaction.ExecuteNonQuery();

                if (updateAccountResult == 1 && insertTransactionResult == 1)
                {
                    transaction.Commit();
                    return true;
                }
            }
            catch (SpringHeroTransactionException e)
            {
                transaction.Rollback();
                return false;
            }

            DbConnection.Instance().CloseConnection();
            return false;
        }

        public bool UpDateBalanceTranfer(Account sendAccount, Account receiveAccount, Transaction historyTransaction)
        {
            DbConnection.Instance().OpenConnection(); // đảm bảo rằng đã kết nối đến db thành công.
            var transaction = DbConnection.Instance().Connection.BeginTransaction(); // Khởi tạo transaction.
            try
            {
                var sendBalance = "select balance from `accounts` where username = @username and status = @status";
                MySqlCommand queryBalanceCommandSend =
                    new MySqlCommand(sendBalance, DbConnection.Instance().Connection);
                queryBalanceCommandSend.Parameters.AddWithValue("@username", sendAccount.Username);
                queryBalanceCommandSend.Parameters.AddWithValue("@status", sendAccount.Status);
                var balanceReaderSend = queryBalanceCommandSend.ExecuteReader();
                // Không tìm thấy tài khoản tương ứng, throw lỗi.
                if (!balanceReaderSend.Read())
                {
                    // Không tồn tại bản ghi tương ứng, lập tức rollback transaction, trả về false.
                    // Hàm dừng tại đây.
                    throw new SpringHeroTransactionException("Invalid username");
                }

                var senderBalanceNum = balanceReaderSend.GetDecimal("balance");
                balanceReaderSend.Close();

                var receiveBalance = "select balance from `accounts` where username = @username and status = @status";
                MySqlCommand queryBalanceCommandReceive =
                    new MySqlCommand(sendBalance, DbConnection.Instance().Connection);
                queryBalanceCommandReceive.Parameters.AddWithValue("@username", receiveAccount.Username);
                queryBalanceCommandReceive.Parameters.AddWithValue("@status", receiveAccount.Status);
                var balanceReaderReceive = queryBalanceCommandReceive.ExecuteReader();
                // Không tìm thấy tài khoản tương ứng, throw lỗi.
                if (!balanceReaderReceive.Read())
                {
                    // Không tồn tại bản ghi tương ứng, lập tức rollback transaction, trả về false.
                    // Hàm dừng tại đây.
                    throw new SpringHeroTransactionException("Invalid username");
                }

                var receiverBalanceNum = balanceReaderReceive.GetDecimal("balance");
                balanceReaderReceive.Close();
                if (historyTransaction.Type == Transaction.TransactionType.TRANSFER)
                {
                    senderBalanceNum -= historyTransaction.Amount;
                    receiverBalanceNum += historyTransaction.Amount;
                }

                var updateAccountResult = 0;
                var queryUpdateSenderAccountBalance =
                    "update `accounts` set balance = @balance where username = @username and status = 1";
                var cmdUpdateSenderAccountBalance =
                    new MySqlCommand(queryUpdateSenderAccountBalance, DbConnection.Instance().Connection);
                cmdUpdateSenderAccountBalance.Parameters.AddWithValue("@username", sendAccount.Username);
                cmdUpdateSenderAccountBalance.Parameters.AddWithValue("@balance", senderBalanceNum);
                cmdUpdateSenderAccountBalance.ExecuteNonQuery();

                var queryUpdateReceiverAccountBalance =
                    "update `accounts` set balance = @balance where username = @username and status = 1";
                var cmdUpdateReceiveAccountBalance =
                    new MySqlCommand(queryUpdateSenderAccountBalance, DbConnection.Instance().Connection);
                cmdUpdateReceiveAccountBalance.Parameters.AddWithValue("@username", receiveAccount.Username);
                cmdUpdateReceiveAccountBalance.Parameters.AddWithValue("@balance", receiverBalanceNum);
                cmdUpdateReceiveAccountBalance.ExecuteNonQuery();

                // 4. Lưu thông tin transaction vào bảng transaction.
                var insertTransactionResult = 0;
                var queryInsertTransaction = "insert into `transactions` " +
                                             "(id, type, amount, content, senderAccountNumber, receiverAccountNumber, status) " +
                                             "values (@id, @type, @amount, @content, @senderAccountNumber, @receiverAccountNumber, @status)";
                var cmdInsertTransaction =
                    new MySqlCommand(queryInsertTransaction, DbConnection.Instance().Connection);
                cmdInsertTransaction.Parameters.AddWithValue("@id", historyTransaction.Id);
                cmdInsertTransaction.Parameters.AddWithValue("@type", historyTransaction.Type);
                cmdInsertTransaction.Parameters.AddWithValue("@amount", historyTransaction.Amount);
                cmdInsertTransaction.Parameters.AddWithValue("@content", historyTransaction.Content);
                cmdInsertTransaction.Parameters.AddWithValue("@senderAccountNumber",
                    historyTransaction.SenderAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@receiverAccountNumber",
                    historyTransaction.ReceiverAccountNumber);
                cmdInsertTransaction.Parameters.AddWithValue("@status", historyTransaction.Status);
                insertTransactionResult = cmdInsertTransaction.ExecuteNonQuery();

                if (insertTransactionResult == 1)
                {
                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception e)
            {
                transaction.Rollback();
                return false;
            }

            DbConnection.Instance().CloseConnection();
            return false;
        }

        public Boolean CheckExistUserName(string username)
        {
            return false;
        }

        public Account GetAccountByUserName(string username)
        {
            DbConnection.Instance().OpenConnection();
            var queryString = "select * from  `accounts` where username = @username and status = 1";
            var cmd = new MySqlCommand(queryString, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@username", username);
            var reader = cmd.ExecuteReader();
            Account account = null;
            if (reader.Read())
            {
                var _username = reader.GetString("username");
                var password = reader.GetString("password");
                var salt = reader.GetString("salt");
                var accountNumber = reader.GetString("accountNumber");
                var identityCard = reader.GetString("identityCard");
                var balance = reader.GetDecimal("balance");
                var phone = reader.GetString("phone");
                var email = reader.GetString("email");
                var fullName = reader.GetString("fullName");
                var createdAt = reader.GetString("createdAt");
                var updatedAt = reader.GetString("updatedAt");
                var status = reader.GetInt32("status");
                account = new Account(_username, password, salt, accountNumber, identityCard, balance, phone, email,
                    fullName, createdAt, updatedAt, (Account.ActiveStatus) status);
            }

            DbConnection.Instance().CloseConnection();
            return account;
        }

        public List<Transaction> GetTransactionHistory10Newest(string accountNumber)
        {
            DbConnection.Instance().OpenConnection();
            List<Transaction> listTransactions = new List<Transaction>();
            var sqlQuery =
                "select * from transactions where senderAccountNumber=@accountNumber or receiverAccountNumber=@accountNumber and DATEDIFF(CURDATE(), createdAt) < 10;";
            var cmd = new MySqlCommand(sqlQuery, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
            var eR = cmd.ExecuteReader();
            while (eR.Read())
            {
                listTransactions.Add(new Transaction(eR.GetString("id"),
                    eR.GetString("createdAt"),
                    eR.GetString("updatedAt"),
                    eR.GetInt32("amount"),
                    eR.GetString("content"),
                    eR.GetString("senderAccountNumber"),
                    eR.GetString("receiverAccountNumber")));
            }
            eR.Close();
            return listTransactions;
        }

        public List<Transaction> GetTransactionHistory5Days(string accountNumber)
        {
            DbConnection.Instance().OpenConnection();
            List<Transaction> listTransactions = new List<Transaction>();
            var sqlQuery =
                "select * from transactions where senderAccountNumber=@accountNumber or receiverAccountNumber=@accountNumber and DATEDIFF(CURDATE(), createdAt) < 5;";
            var cmd = new MySqlCommand(sqlQuery, DbConnection.Instance().Connection);
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
            var eR = cmd.ExecuteReader();
            while (eR.Read())
            {
                listTransactions.Add(new Transaction(eR.GetString("id"),
                    eR.GetString("createdAt"),
                    eR.GetString("updatedAt"),
                    eR.GetInt32("amount"),
                    eR.GetString("content"),
                    eR.GetString("senderAccountNumber"),
                    eR.GetString("receiverAccountNumber")));
            }
            eR.Close();
            return listTransactions;
        }
    }
}