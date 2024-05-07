using FluentResults;

namespace ChatApp_Server.Helper
{
    public static class ExceptionHandler
    {
        public async static Task<Result<T>> HandleLazy<T>(Func<Task<Result<T>>> wraper)
        {
			try
			{
				var result = await wraper();
				return result;
			}
			catch (Exception ex)
			{
				return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.ToString());
			}
        }
        public async static Task<Result> HandleLazy(Func<Task<Result>> wraper)
        {
            try
            {
                var result = await wraper();
                return result;
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.ToString());
            }
        }
    }
}
