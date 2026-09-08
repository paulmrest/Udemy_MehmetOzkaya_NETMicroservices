using Discount.Grpc.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Mapster;
using Discount.Grpc.Models;

namespace Discount.Grpc.Services;

public class DiscountService
  (DiscountContext dbContext, ILogger<DiscountService> logger)
  : DiscountProtoService.DiscountProtoServiceBase
{
  public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
  {
    var coupon = await dbContext
      .Coupons
      .FirstOrDefaultAsync(c => c.ProductName == request.ProductName);
    if (coupon == null)
    {
      coupon = new Models.Coupon { ProductName = "No Discount", Amount = 0, Description = "No Discount Desc" };
    }

    logger.LogInformation("Discount is retrieved for ProductName : {productName}, Amount : {amount}", coupon.ProductName, coupon.Amount);

    return coupon.Adapt<CouponModel>();
  }

  public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
  {
    var coupon = request.Coupon.Adapt<Coupon>();
    if (coupon == null)
    {
      throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request object."));
    }
    dbContext.Coupons.Add(coupon);
    await dbContext.SaveChangesAsync();
    logger.LogInformation("Discount successfully created. ProductName : {productName}", coupon.ProductName);

    return coupon.Adapt<CouponModel>();
  }

  public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
  {
    var coupon = request.Coupon.Adapt<Coupon>();
    if (coupon == null)
    {
      throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request object."));
    }
    dbContext.Coupons.Update(coupon);
    await dbContext.SaveChangesAsync();

    logger.LogInformation("Discount successfully updated. ProductName : {productName}", coupon.ProductName);

    return coupon.Adapt<CouponModel>();
  }

  public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
  {
    var coupon = await dbContext.Coupons.FirstOrDefaultAsync(c => c.ProductName == request.ProductName);
    if (coupon == null)
    {
      throw new RpcException(new Status(StatusCode.NotFound, $"Discount with ProductName={request.ProductName} is not found."));
    }
    dbContext.Coupons.Remove(coupon);
    await dbContext.SaveChangesAsync();

    logger.LogInformation("Discount successfully removed. ProductName : {productName}", coupon.ProductName);

    return new DeleteDiscountResponse { Success = true };
  }
}
