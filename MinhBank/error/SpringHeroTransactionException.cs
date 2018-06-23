using System;

namespace MinhBank.error
{
    public class SpringHeroTransactionException: Exception
    {
        public SpringHeroTransactionException(string message) : base(message)
        {
        }
    }
}