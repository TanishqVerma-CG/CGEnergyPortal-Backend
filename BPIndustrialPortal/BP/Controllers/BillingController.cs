using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers
{
    [ApiController]
    [Authorize(Policy = "Billing")]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;

        public BillingController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpGet]
        [Route("api/GetBillingSummary")]
        [ProducesResponseType(typeof(BillingSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetBillingSummary()
        {
            try
            {
                return Ok(_billingService.GetSummary());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetOutstandingInvoices")]
        [ProducesResponseType(typeof(OutstandingInvoicesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetOutstandingInvoices()
        {
            try
            {
                return Ok(_billingService.GetOutstandingInvoices());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetInvoiceHistory")]
        [ProducesResponseType(typeof(InvoiceHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetInvoiceHistory()
        {
            try
            {
                return Ok(_billingService.GetInvoiceHistory());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetPaymentHistory")]
        [ProducesResponseType(typeof(PaymentHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPaymentHistory()
        {
            try
            {
                return Ok(_billingService.GetPaymentHistory());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetInvoiceDetail")]
        [ProducesResponseType(typeof(InvoiceDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetInvoiceDetail([FromQuery][Required] string invoiceId)
        {
            try
            {
                var result = _billingService.GetInvoiceDetail(invoiceId);

                if (result is null)
                    return NotFound(new { message = $"Invoice '{invoiceId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/MakePayment")]
        [ProducesResponseType(typeof(MakePaymentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult MakePayment([FromBody] MakePaymentRequest request)
        {
            try
            {
                var result = _billingService.MakePayment(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetContractBillingSummary")]
        [ProducesResponseType(typeof(ContractBillingSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractBillingSummary([FromQuery] string contractId)
        {
            try
            {
                var result = _billingService.GetContractBillingSummary(contractId);

                if (result is null)
                    return NotFound(new { message = $"Billing summary for contract '{contractId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}
