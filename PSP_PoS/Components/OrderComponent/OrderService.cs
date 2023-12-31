﻿using Microsoft.EntityFrameworkCore;
using PSP_PoS.Components.ItemComponent;
using PSP_PoS.Components.ItemOrderComponent;
using PSP_PoS.Components.OrderItemsComponent;
using PSP_PoS.Components.OrderService;
using PSP_PoS.Components.OrderServicesComponent;
using PSP_PoS.Components.ServiceComponent;
using PSP_PoS.Data;
using PSP_PoS.Enums;
using PSP_PoS.OtherDtos;
using System.Linq;

namespace PSP_PoS.Components.OrderComponent
{
    public class OrderService: IOrderService
    {
        private readonly DataContext _context;

        public OrderService(DataContext context)
        {
            _context = context;
        }  
        
        public Order AddOrder(OrderCreateDto orderCreateDto)
        {
            Order order = new Order(orderCreateDto);
            _context.Orders.Add(order);
            _context.SaveChanges();
            return order;
        }

        public OrderItems AddItemToOrder(Guid orderId, Guid itemId)
        {
            OrderItems? existingOrderItem = _context.OrderItems
                .FirstOrDefault(o => o.OrderId == orderId && o.ItemId == itemId);

            if (existingOrderItem != null)
            {
                existingOrderItem.Quantity++;
                _context.OrderItems.Update(existingOrderItem);
                _context.SaveChanges();
                return existingOrderItem;
            }
            else
            {
                Item item = _context.Items.FirstOrDefault(i => i.Id == itemId)!;
                item.Stock--;
                _context.Items.Update(item);

                OrderItems newOrderItem = new OrderItems(orderId, itemId, 1);
                _context.OrderItems.Add(newOrderItem);
                _context.SaveChanges();
                return newOrderItem;
            }
        }
        public OrderServices AddServiceToOrder(Guid orderId, Guid serviceId)
        {
            OrderServices? existingOrderService = _context.OrderServices
                .FirstOrDefault(o => o.OrderId == orderId && o.ServiceId == serviceId);

            if (existingOrderService != null)
            {
                existingOrderService.Quantity++;
                _context.OrderServices.Update(existingOrderService);
                _context.SaveChanges();
                return existingOrderService;
            }
            else
            {
                OrderServices newOrderService = new OrderServices(orderId, serviceId, 1);
                _context.OrderServices.Add(newOrderService);
                _context.SaveChanges();
                return newOrderService;
            }
        }

        public bool AddTaxToOrder(Guid orderId, Guid taxId)
        {
            Order? order = _context.Orders.Include(o => o.Tax).FirstOrDefault(o => o.Id == orderId);
            order.TaxId = taxId;
            order.Tax = _context.Taxes.Find(taxId);
            _context.Orders.Update(order);
            _context.SaveChanges();
            return true;
        }
        
        public bool RemoveTaxFromOrder(Guid orderId)
        {
            Order? order = _context.Orders.Find(orderId);
            order.TaxId = null;
            order.Tax = null;
            _context.Orders.Update(order);
            _context.SaveChanges();
            return true;
        }

        public OrderReadDto GetOrderById(Guid orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId)!;

            List<OrderItemIdDto> orderItems = _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => new OrderItemIdDto
                {
                    ItemId = oi.ItemId,
                    Quantity = oi.Quantity
                })
                .ToList();

            List<OrderServiceIdDto> orderServices = _context.OrderServices
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => new OrderServiceIdDto
                {
                    ServiceId = oi.ServiceId,
                    Quantity = oi.Quantity
                })
                .ToList();

            OrderReadDto orderReadDto = new OrderReadDto(order, orderItems, orderServices);
            return orderReadDto;
        }

        public List<OrderReadDto> GetAllOrders()
        {
            var allOrders = _context.Orders.ToList();

            List<OrderReadDto> orderReadDtos = new List<OrderReadDto>();

            foreach (var order in allOrders)
            {
                List<OrderItemIdDto> orderItems = _context.OrderItems
                    .Where(oi => oi.OrderId == order.Id)
                    .Select(oi => new OrderItemIdDto
                    {
                        ItemId = oi.ItemId,
                        Quantity = oi.Quantity
                    })
                    .ToList();

                List<OrderServiceIdDto> orderServices = _context.OrderServices
                    .Where(os => os.OrderId == order.Id)
                    .Select(os => new OrderServiceIdDto
                    {
                        ServiceId = os.ServiceId,
                        Quantity = os.Quantity
                    })
                    .ToList();

                OrderReadDto orderReadDto = new OrderReadDto(order, orderItems, orderServices);
                if(orderReadDto.OrderStatus != "Finished")
                {
                    orderReadDtos.Add(orderReadDto);
                }
                
            }

            return orderReadDtos;
        }

        public bool RemoveItemFromOrder(Guid orderId, Guid itemId)
        {
            OrderItems existingOrderItem = _context.OrderItems
                .FirstOrDefault(o => o.OrderId == orderId && o.ItemId == itemId)!;

            if (existingOrderItem != null)
            {
                if (existingOrderItem.Quantity > 1)
                {
                    existingOrderItem.Quantity--;
                    _context.OrderItems.Update(existingOrderItem);
                }
                else
                {
                    _context.OrderItems.Remove(existingOrderItem);
                }
                Item item = _context.Items.FirstOrDefault(i => i.Id == itemId)!;
                item.Stock++;
                _context.Items.Update(item);

                _context.SaveChanges();
                return true;
            }
            else
            { 
                return false;
            }
        }

        public bool RemoveServiceFromOrder(Guid orderId, Guid serviceId)
        {
            OrderServices existingOrderService = _context.OrderServices
                .FirstOrDefault(o => o.OrderId == orderId && o.ServiceId == serviceId)!;

            if (existingOrderService != null)
            {
                _context.OrderServices.Remove(existingOrderService);
                _context.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public Cheque GenerateCheque(Guid orderId)
        {
            Order order = _context.Orders.Include(o => o.Tax).FirstOrDefault(o => o.Id == orderId)!;
            List<OrderItems> orderItems = _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToList();
            List<OrderServices> orderServices = _context.OrderServices
                    .Where(oi => oi.OrderId == orderId)
                    .ToList();
            List<ItemCheque> itemCheques = _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Join(
                        _context.Items,
                        orderItem => orderItem.ItemId,
                        item => item.Id,
                        (orderItem, item) => new ItemCheque
                        {
                            Name = item.Name,
                            Price = Math.Round(item.Price / 100m, 2),
                            Quantity = orderItem.Quantity,
                            DiscountPercentage = (item.Discount != null) ? item.Discount.Percentage : 0,
                            PriceAfterDiscount = (item.Discount != null)
                                ? Math.Round((item.Price / 100) - (item.Price / 100 * ((decimal)item.Discount.Percentage / 100)), 2)
                                : Math.Round(item.Price / 100, 2)
                        }
                    )
                    .ToList();

            List<ServiceCheque> serviceCheques = _context.OrderServices
                    .Where(oi => oi.OrderId == orderId)
                    .Join(
                        _context.Services,
                        orderService => orderService.ServiceId,
                        service => service.Id,
                        (orderService, service) => new ServiceCheque
                        {
                            Name = service.Name,
                            Price = Math.Round(service.Price / 100, 2), 
                            Quantity = orderService.Quantity,
                            DiscountPercentage = (service.Discount != null) ? service.Discount.Percentage : 0,
                            PriceAfterDiscount = (service.Discount != null)
                                ? Math.Round((service.Price / 100) - (service.Price / 100 * ((decimal)service.Discount.Percentage / 100)), 2)
                                : Math.Round(service.Price / 100, 2)  
                        }
                    )
                    .ToList();


            Cheque cheque = new Cheque(order, itemCheques, serviceCheques);
            return cheque;
        }

        public void DeleteOrder(Guid orderId)
        {
            Order order = _context.Orders.FirstOrDefault(o => o.Id == orderId)!;
            if(order != null)
            {
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }
        }

        public bool PayForOrder(Guid orderId, PaymentType payment)
        {
            Order order = _context.Orders.FirstOrDefault(o => o.Id == orderId)!;
            order.PaymentType = payment;
            order.OrderStatus = Status.Paid;
            _context.Orders.Update(order);
            _context.SaveChanges();
            return true;
        }

        public bool AddTip(Guid orderId, decimal amount)
        {
            Order order = _context.Orders.FirstOrDefault(o => o.Id == orderId)!;
            
            if(order.OrderStatus == Status.Paid)
            {
                order.OrderStatus = Status.PaidAndTipped;
                order.Tip = amount;
                _context.Orders.Update(order);
                _context.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public bool CloseOrder(Guid orderId)
        {
            Order order = _context.Orders.FirstOrDefault(o => o.Id == orderId)!;

            if (order.OrderStatus == Status.Paid || order.OrderStatus == Status.PaidAndTipped)
            {
                order.OrderStatus = Status.Finished;
                _context.Orders.Update(order);
                _context.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IfCustomerIdValid(Guid id)
        {
            var customer = _context.Customers.Find(id);
            if(customer != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IfEmployeeIdValid(Guid id)
        {
            var employee = _context.Employees.Find(id);
            if (employee != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IfItemIdValid(Guid id)
        {
            var item = _context.Items.Find(id);
            if (item != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IfServiceIdValid(Guid id)
        {
            var service = _context.Services.Find(id);
            if (service != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IfOrderIdValid(Guid id)
        {
            var order = _context.Orders.Find(id);
            if (order != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IfItemExists(Guid itemId)
        {
            var item = _context.Items.Find(itemId)!;
            if(item.Stock > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
