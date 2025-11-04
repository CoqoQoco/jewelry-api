## payment item

 ## domain ที่เกี่ยวข้อง E:\coqo_duangkeaw\Code\jewelry-api\Jewelry.Api\Jewelry.Data\Models\Jewelry\TbtSaleInvoicePaymentItem.cs

 ## สร้าง function payment module ดังนี้ ที่ E:\coqo_duangkeaw\Code\jewelry-api\Jewelry.Api\Jewelry.Service\Sale\Invoice\InvoiceService.cs

   ## 1 fuction craete payment
       - create date 
       - payment method
       - amount
       - anount unit from invoice header
       - reference no
       - ref image see ex E:\coqo_duangkeaw\Code\jewelry-api\Jewelry.Api\Jewelry.Service\Stock\ProductImage\ProductImageService.cs
       - bank name

  ## 2 function litd payment by invoice no
       - invoice no
       - return list payment item

  ## 3 function delete payment by payment running no, invoice no, so no
       - payment running no
       - return string