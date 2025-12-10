using System;
using System.Linq;
using FusionComms.Data;
using FusionComms.Entities;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace FusionComms.Pages
{
    public class MailJetMessagesModel : PageModel
    {
        private readonly AppDbContext _context; 

        public MailJetMessagesModel(AppDbContext context)
        {
            _context = context;
        }

        public List<SentEmail> SentMessages { get; set; }

        public void OnGet()
        {
            SentMessages = _context.SentEmails
                .OrderByDescending(e => e.SentAt)
                .ToList();
        }
    }
}