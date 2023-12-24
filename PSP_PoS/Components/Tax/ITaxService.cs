﻿namespace PSP_PoS.Components.Tax
{
    public interface ITaxService
    {
        List<Tax> GetAllTaxes();
        Tax? GetTaxById(Guid taxId);
        Tax AddTax(TaxDto tax);
        bool UpdateTax(TaxDto tax, Guid id);
        void DeleteTax(Guid taxId);
    }
}
