using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

using BrickEmulator.Security;
using BrickEmulator.Storage;

namespace BrickEmulator.HabboHotel.Shop
{
    class VoucherReactor
    {
        public Dictionary<string, short> Vouchers;

        public VoucherReactor() { }

        public void Prepare()
        {
            Vouchers = new Dictionary<string, short>();

            DataTable Table = null;

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("SELECT * FROM voucher_codes");
                Table = Reactor.GetTable();
            }

            if (Table != null)
            {
                foreach (DataRow Row in Table.Rows)
                {
                    SecurityCounter Counter = new SecurityCounter(0);

                    string DecodedVoucher = BrickEngine.GetConvertor().ObjectToString(Row[Counter.Next]);
                    short CreditCode = BrickEngine.GetConvertor().ObjectToShort(Row[Counter.Next]);

                    if (CreditCode < 0)
                    {
                        CreditCode = 0;
                    }

                    if (!Vouchers.ContainsKey(DecodedVoucher))
                    {
                        Vouchers.Add(DecodedVoucher, CreditCode);
                    }
                    else
                    {
                        Vouchers[DecodedVoucher] = CreditCode;
                    }
                }
            }

            BrickEngine.GetScreenWriter().ScretchLine("[" + Vouchers.Count + "] Voucher(s) cached.", IO.WriteType.Outgoing);
        }

        public void AddVoucher(string Code, short Amount)
        {
            if (!Vouchers.ContainsKey(Code))
            {
                Vouchers.Add(Code, Amount);
            }
            else
            {
                Vouchers[Code] = Amount;
            }

            using (QueryReactor Reactor = BrickEngine.GetQueryReactor())
            {
                Reactor.SetQuery("INSERT INTO voucher_codes (voucher_hex,credits_reward) VALUES (@code,@amount)");
                Reactor.AddParam("code", Code);
                Reactor.AddParam("amount", Amount);
                Reactor.ExcuteQuery();
            }
        }

        public Boolean CheckVoucher(string Code)
        {
            if (!Vouchers.ContainsKey(Code))
            {
                return false;
            }

            return true;
        }
    }
}
