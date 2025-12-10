using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FusionComms.Data;
using FusionComms.DTOs.WhatsApp;

namespace FusionComms.Services.WhatsApp.Cinemas
{
    public class CinemaWebhookProcessor : WhatsAppWebhookProcessor
    {
        private readonly WhatsAppMessagingService _whatsAppMessaging;

        public CinemaWebhookProcessor(AppDbContext dbContext, WhatsAppMessagingService whatsAppMessaging)
            : base(dbContext)
        {
            _whatsAppMessaging = whatsAppMessaging;
        }

        protected override async Task OnTextMessageReceived(WhatsAppMessageEvent messageEvent)
        {
            var content = messageEvent.Content?.Trim().ToLower();

            switch (content)
            {
                case "hi":
                case "hello":
                case "hey":
                    await SendWelcomeMessage(messageEvent);
                    break;
                    
                case "showtimes":
                case "movies":
                case "what's playing":
                    await SendShowtimesInteractiveList(messageEvent);
                    break;
                    
                case "book":
                case "tickets":
                case "reserve":
                    await SendGenreOptions(messageEvent);
                    break;
                    
                case "help":
                case "menu":
                    await SendHelpMenu(messageEvent);
                    break;
                    
                default:
                    await SendDefaultResponse(messageEvent);
                    break;
            }
        }

        protected override async Task OnInteractiveButtonClicked(WhatsAppMessageEvent messageEvent)
        {
            var buttonId = messageEvent.InteractivePayload;
            
            switch (buttonId)
            {
                case "btn_showtimes":
                    await SendShowtimesInteractiveList(messageEvent);
                    break;
                    
                case "btn_book_tickets":
                    await SendMovieSelectionList(messageEvent);
                    break;
                    
                case "btn_contact":
                    await SendContactInfo(messageEvent);
                    break;
                    
                case "btn_help":
                    await SendHelpMenu(messageEvent);
                    break;

                case "btn_genres":
                    await SendGenreOptions(messageEvent);
                    break;
                    
                case "btn_popcorn":
                case "btn_action":
                case "btn_comedy":
                case "btn_drama":
                    await SendMoviesByGenre(messageEvent, buttonId);
                    break;
                    
                default:
                    await HandleUnknownButton(messageEvent);
                    break;
            }
        }

        protected override async Task OnInteractiveListSelected(WhatsAppMessageEvent messageEvent)
        {
            var selection = messageEvent.InteractivePayload;
            
            if (selection.StartsWith("movie_"))
            {
                await HandleMovieSelection(messageEvent, selection);
            }
            else if (selection.StartsWith("time_"))
            {
                await HandleTimeSelection(messageEvent, selection);
            }
            else if (selection.StartsWith("tickets_"))
            {
                await HandleTicketQuantitySelection(messageEvent, selection);
            }
            else
            {
                await SendUnknownSelectionResponse(messageEvent);
            }
        }

        private async Task SendWelcomeMessage(WhatsAppMessageEvent messageEvent)
        {
            var customerName = messageEvent.CustomerName ?? "there";
            
            var buttons = new List<WhatsAppButton>
            {
                new() { Payload = "btn_showtimes", Text = "🎬 Showtimes" },
                new() { Payload = "btn_book_tickets", Text = "🎟️ Book Tickets" },
                new() { Payload = "btn_contact", Text = "📞 Contact" }
            };

            await _whatsAppMessaging.SendInteractiveMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                $"Welcome to CineMax, {customerName}! 🍿\n\nWhat would you like to do today?",
                buttons,
                "Your ultimate cinema experience"
            );
        }

        private async Task SendShowtimesInteractiveList(WhatsAppMessageEvent messageEvent)
        {
            var sections = new List<WhatsAppSection>
            {
                new() {
                    Title = "🎥 Now Playing",
                    Rows = new List<WhatsAppRow>
                    {
                        new() { Id = "movie_avengers", Title = "Avengers: Endgame", Description = "2:00 PM, 5:00 PM, 8:00 PM" },
                        new() { Id = "movie_batman", Title = "The Batman", Description = "3:00 PM, 6:00 PM, 9:00 PM" },
                        new() { Id = "movie_dune", Title = "Dune: Part Two", Description = "1:30 PM, 4:30 PM, 7:30 PM" }
                    }
                },
                new() {
                    Title = "🎭 Coming Soon",
                    Rows = new List<WhatsAppRow>
                    {
                        new() { Id = "movie_spiderman", Title = "Spider-Man: No Way Home", Description = "Starts Friday" },
                        new() { Id = "movie_avatar", Title = "Avatar 3", Description = "Coming Next Month" }
                    }
                }
            };

            await _whatsAppMessaging.SendInteractiveListAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "🎬 Today's Showtimes\n\nSelect a movie to see available times and book tickets:",
                "Select Movie",
                sections
            );
        }

        private async Task SendGenreOptions(WhatsAppMessageEvent messageEvent)
        {
            var buttons = new List<WhatsAppButton>
            {
                new() { Payload = "btn_action", Text = "💥 Action" },
                new() { Payload = "btn_comedy", Text = "😂 Comedy" },
                new() { Payload = "btn_drama", Text = "🎭 Drama" }
            };

            await _whatsAppMessaging.SendInteractiveMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "Great! Let's find your perfect movie experience. Choose a category:",
                buttons,
                "Browse by genre"
            );
        }

        private async Task SendMovieSelectionList(WhatsAppMessageEvent messageEvent)
        {
            var sections = new List<WhatsAppSection>
            {
                new WhatsAppSection
                {
                    Title = "🌟 Featured Movies",
                    Rows = new List<WhatsAppRow>
                    {
                        new() { Id = "movie_avengers_book", Title = "Avengers: Endgame", Description = "Action • PG-13 • 3h 1m" },
                        new() { Id = "movie_batman_book", Title = "The Batman", Description = "Action • PG-13 • 2h 56m" },
                        new() { Id = "movie_dune_book", Title = "Dune: Part Two", Description = "Sci-Fi • PG-13 • 2h 46m" }
                    }
                }
            };

            await _whatsAppMessaging.SendInteractiveListAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "Select a movie to book tickets:",
                "Book Tickets",
                sections
            );
        }

        private async Task SendMoviesByGenre(WhatsAppMessageEvent messageEvent, string genreId)
        {
            var genreName = genreId switch
            {
                "btn_action" => "Action",
                "btn_comedy" => "Comedy",
                "btn_drama" => "Drama",
                _ => "Movies"
            };

            var movies = genreId switch
            {
                "btn_action" => new[]
                {
                    new { Id = "movie_avengers_act", Title = "Avengers: Endgame", Desc = "Action • 3h 1m" },
                    new { Id = "movie_batman_act", Title = "The Batman", Desc = "Action • 2h 56m" }
                },
                "btn_comedy" => new[]
                {
                    new { Id = "movie_comedy1", Title = "Super Funny Movie", Desc = "Comedy • 1h 45m" }
                },
                "btn_drama" => new[]
                {
                    new { Id = "movie_drama1", Title = "The Great Story", Desc = "Drama • 2h 15m" }
                },
                _ => Array.Empty<dynamic>()
            };

            var section = new WhatsAppSection
            {
                Title = $"🎬 {genreName} Movies",
                Rows = new List<WhatsAppRow>()
            };

            foreach (var movie in movies)
            {
                section.Rows.Add(new WhatsAppRow 
                { 
                    Id = movie.Id, 
                    Title = movie.Title, 
                    Description = movie.Desc 
                });
            }

            await _whatsAppMessaging.SendInteractiveListAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                $"Great choice! Here are our {genreName.ToLower()} movies:",
                "Select Movie",
                new List<WhatsAppSection> { section }
            );
        }

        private async Task HandleMovieSelection(WhatsAppMessageEvent messageEvent, string movieId)
        {
            var movieName = movieId switch
            {
                "movie_avengers" or "movie_avengers_book" or "movie_avengers_pop" or "movie_avengers_act" => "Avengers: Endgame",
                "movie_batman" or "movie_batman_book" or "movie_batman_pop" or "movie_batman_act" => "The Batman",
                "movie_dune" or "movie_dune_book" => "Dune: Part Two",
                "movie_spiderman" => "Spider-Man: No Way Home",
                "movie_avatar" => "Avatar 3",
                "movie_comedy1" => "Super Funny Movie",
                "movie_drama1" => "The Great Story",
                _ => "Selected Movie"
            };

            var sections = new List<WhatsAppSection>
            {
                new WhatsAppSection
                {
                    Title = "🕐 Show Times",
                    Rows = new List<WhatsAppRow>
                    {
                        new() { Id = "time_2pm", Title = "2:00 PM", Description = "Standard • ₦12.99" },
                        new() { Id = "time_5pm", Title = "5:00 PM", Description = "Standard • ₦12.99" },
                        new() { Id = "time_8pm", Title = "8:00 PM", Description = "Premium • ₦15.99" }
                    }
                }
            };

            await _whatsAppMessaging.SendInteractiveListAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                $"Great choice! {movieName} 🎬\n\nSelect a show time:",
                "Select Time",
                sections
            );
        }

        private async Task HandleTimeSelection(WhatsAppMessageEvent messageEvent, string timeId)
        {
            var time = timeId switch
            {
                "time_2pm" => "2:00 PM",
                "time_5pm" => "5:00 PM",
                "time_8pm" => "8:00 PM",
                _ => "selected time"
            };

            var sections = new List<WhatsAppSection>
            {
                new() {
                    Title = "🎟️ Ticket Quantity",
                    Rows = new List<WhatsAppRow>
                    {
                        new() { Id = "tickets_1", Title = "1 Ticket", Description = "Total: ₦12.99" },
                        new() { Id = "tickets_2", Title = "2 Tickets", Description = "Total: ₦25.98" },
                        new() { Id = "tickets_3", Title = "3 Tickets", Description = "Total: ₦38.97" },
                        new() { Id = "tickets_4", Title = "4 Tickets", Description = "Total: ₦51.96" }
                    }
                }
            };

            await _whatsAppMessaging.SendInteractiveListAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                $"Perfect! {time} show time selected.\n\nHow many tickets would you like?",
                "Select Quantity",
                sections
            );
        }

        private async Task HandleTicketQuantitySelection(WhatsAppMessageEvent messageEvent, string ticketId)
        {
            var ticketCount = ticketId switch
            {
                "tickets_1" => "1 ticket",
                "tickets_2" => "2 tickets",
                "tickets_3" => "3 tickets",
                "tickets_4" => "4 tickets",
                _ => "your tickets"
            };

            await _whatsAppMessaging.SendTextMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                $"🎉 Excellent! You've selected {ticketCount}.\n\n" +
                "To complete your booking, please:\n" +
                "1. Visit our website: cinemax.com/bookings\n" +
                "2. Call us: +1-555-CINEMA\n" +
                "3. Visit our box office\n\n" +
                "Your selected show will be held for 15 minutes. 🍿"
            );

            var buttons = new List<WhatsAppButton>
            {
                new() { Payload = "btn_showtimes", Text = "🎬 More Movies" },
                new() { Payload = "btn_genres", Text = "🎞️ View Genres" },
                new() { Payload = "btn_contact", Text = "📞 Call Us" }
            };

            await _whatsAppMessaging.SendInteractiveMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "Need anything else?",
                buttons,
                "We're here to help!"
            );
        }

        private async Task SendHelpMenu(WhatsAppMessageEvent messageEvent)
        {
            var buttons = new List<WhatsAppButton>
            {
                new() { Payload = "btn_showtimes", Text = "🎬 Showtimes" },
                new() { Payload = "btn_book_tickets", Text = "🎟️ Book Tickets" },
                new() { Payload = "btn_contact", Text = "📞 Contact Info" }
            };

            await _whatsAppMessaging.SendInteractiveMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "How can I help you today? Choose an option below:",
                buttons,
                "CineMax Support"
            );
        }

        private async Task SendContactInfo(WhatsAppMessageEvent messageEvent)
        {
            await _whatsAppMessaging.SendTextMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "📞 **CineMax Contact Information**\n\n" +
                "📍 Address: 123 Movie Street, Cinema City\n" +
                "📞 Phone: +1-555-CINEMA (246362)\n" +
                "📧 Email: info@cinemax.com\n" +
                "🌐 Website: www.cinemax.com\n\n" +
                "🕒 **Box Office Hours:**\n" +
                "Monday-Friday: 10AM-11PM\n" +
                "Weekends: 9AM-Midnight\n\n" +
                "Need immediate assistance? Call us now! 📞"
            );
        }

        private async Task SendDefaultResponse(WhatsAppMessageEvent messageEvent)
        {
            var buttons = new List<WhatsAppButton>
            {
                new WhatsAppButton { Payload = "btn_showtimes", Text = "🎬 Showtimes" },
                new WhatsAppButton { Payload = "btn_book_tickets", Text = "🎟️ Book Tickets" },
                new WhatsAppButton { Payload = "btn_help", Text = "❓ Help" }
            };

            await _whatsAppMessaging.SendInteractiveMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "I'm here to help you with cinema bookings and showtimes! 🎭\n\n" +
                "You can ask me about:\n" +
                "• Today's movie showtimes\n" +
                "• Booking tickets\n" +
                "• Cinema locations\n" +
                "• Or use the quick options below:",
                buttons,
                "Your cinema assistant"
            );
        }

        private async Task HandleUnknownButton(WhatsAppMessageEvent messageEvent)
        {
            await _whatsAppMessaging.SendTextMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "I'm not sure what you're trying to do with that option. Let me show you what I can help with!"
            );

            await SendHelpMenu(messageEvent);
        }

        private async Task SendUnknownSelectionResponse(WhatsAppMessageEvent messageEvent)
        {
            await _whatsAppMessaging.SendTextMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "I didn't understand that selection. Let's start over!"
            );

            await SendWelcomeMessage(messageEvent);
        }

        protected override async Task OnImageReceived(WhatsAppMessageEvent messageEvent)
        {
            await _whatsAppMessaging.SendTextMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "Thanks for the image! 📸\n\n" +
                "I'm focused on helping you with movie bookings and showtimes. " +
                "Try sending 'showtimes' to see what's playing today!"
            );
        }

        protected override async Task OnVideoReceived(WhatsAppMessageEvent messageEvent)
        {
            await _whatsAppMessaging.SendTextMessageAsync(
                messageEvent.BusinessId,
                messageEvent.PhoneNumber,
                "Thanks for the video! 🎥\n\n" +
                "While I can't process videos, I'd love to help you book movie tickets or check showtimes. " +
                "Just type 'book' to get started!"
            );
        }
    }
}