namespace PaymentPlatform

open System.Threading.Tasks

type UserId = UserId of string

type Friend = { Id: string; Name: string }

type IPaymentPlatform =
  abstract ListFriends: unit -> Task<Friend list>

type PaymentPlatformFactory = UserId -> Task<IPaymentPlatform option>

type IPaymentPlatformFactory =
  abstract Get: UserId -> Task<IPaymentPlatform option>