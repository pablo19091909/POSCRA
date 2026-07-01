using PulperiaPOS.Models.Api;

namespace PulperiaPOS.Models.Caja
{
    public sealed class CajaCierreResult
    {
        public CajaCierreResult(ApiRequestResult<CierreCajaApiResponse> apiResult)
        {
            ApiResult = apiResult;
        }

        public ApiRequestResult<CierreCajaApiResponse> ApiResult { get; }
        public bool Success => ApiResult.Success;
        public CierreCajaApiResponse? Cierre => ApiResult.Data;
    }
}
