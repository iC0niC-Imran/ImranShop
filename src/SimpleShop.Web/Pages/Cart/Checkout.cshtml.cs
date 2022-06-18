using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleShop.Application.Commands.Carts;
using SimpleShop.Application.Commands.Orders;
using SimpleShop.Application.Queries.Carts;
using SimpleShop.Domain.Entities;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace SimpleShop.Web.Pages.Cart
{
    public class CheckoutModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutModel(IMediator mediator, UserManager<ApplicationUser> userManager)
        {
            _mediator = mediator;
            _userManager = userManager;
        }

        public Domain.Entities.Cart Cart { get; set; }
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Wrong name format.")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last name is required.")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Wrong name format.")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "The street name is required.")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Wrong street name format.")]
            public string Street { get; set; }

            [Required(ErrorMessage = "The building / flat number is required.")]
            [RegularExpression(@"[1-9]\d*(\s*[-/]\s*[1-9]\d*)?(\s?[a-zA-Z])?", ErrorMessage = "Incorrect format of the building / apartment number.")]
            public string Apartment { get; set; }

            [Required(ErrorMessage = "The name of the city is required.")]
            [RegularExpression(@"^[^0-9_!¡?÷?¿/\\+=@#$%ˆ&*(){}|~<>;:[\]]{2,}$", ErrorMessage = "Wrong format of the city name.")]
            public string City { get; set; }

            [Required(ErrorMessage = "Zip code is required.")]
            [RegularExpression(@"^[a-z0-9][a-z0-9\- ]{0,10}[a-z0-9]$", ErrorMessage = "Wrong postal code format.")]
            public string PostalCode { get; set; }

            [Required(ErrorMessage = "A mobile number is required.")]
            [RegularExpression(@"(?<!\w)(\(?(\+|00)?48\)?)?[ -]?\d{3}[ -]?\d{3}[ -]?\d{3}(?!\w)", ErrorMessage = "Invalid mobile number format.")]
            public string Phone { get; set; }

            [Required(ErrorMessage = "E-mail address is required.")]
            [EmailAddress(ErrorMessage = "Invalid e-mail address format.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Payment method is required.")]
            public string PaymentMethod { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            Cart = await _mediator.Send(new GetCart.Query(userId));
            if (Cart.IsEmpty)
            {
                return RedirectToPage("Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);

            Cart = await _mediator.Send(new GetCart.Query(userId));
            if (Cart.IsEmpty)
            {
                return RedirectToPage("Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Number = new Random().Next(1000, 4000)
            };

            var success = await _mediator.Send(new CreateOrder.Command(order));
            if (success)
            {
                var orderItems = new List<OrderItem>();
                foreach (var cartItem in Cart.Items)
                {
                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = cartItem.ProductId,
                        OrderId = order.Id,
                        Quantity = cartItem.Quantity,
                        Size = cartItem.Size
                    });
                }

                var orderDetails = new OrderDetails
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Street = Input.Street,
                    Apartment = Input.Apartment,
                    City = Input.City,
                    PostalCode = Input.PostalCode,
                    Phone = Input.Phone,
                    Email = Input.Email,
                    PaymentMethod = Input.PaymentMethod,
                    Total = Cart.GetTotal()
                };

                await _mediator.Send(new AddOrderItems.Command(orderItems));
                await _mediator.Send(new AddOrderDetails.Command(orderDetails));
                await _mediator.Send(new ClearCart.Command(Cart.Id));
            }

            return RedirectToPage("Summary", new { orderId = order.Id });
        }
    }
}
