using PulperiaPOS.Models.Api;

namespace PulperiaPOS.Models.Caja
{
    public sealed class CajaRetiroResult
    {
        public CajaRetiroResult(ApiRequestResult<MovimientoCajaApiResponse> apiResult)
        {
            ApiResult = apiResult;
        }

        public ApiRequestResult<MovimientoCajaApiResponse> ApiResult { get; }
        public bool Success => ApiResult.Success;
        public MovimientoCajaApiResponse? Movimiento => ApiResult.Data;
    }
}
