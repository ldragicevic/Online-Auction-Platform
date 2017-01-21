using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using FluentScheduler;
using IEP_Projekat.Models;
using System.Threading;
using System.Data.Entity;
using System.Text;
using System.Web.Mail;
using System.Net.Mail;
using System.Collections;
using Microsoft.Ajax.Utilities;

namespace IEP_Projekat.Hubs
{
    
    public static class mutex
    {
        public static string lockObject = "MutEx";
    }

    public class MyHub : Hub
    {

        private static Dictionary<string, string> hashUsersConnIds = new Dictionary<string, string>(512);
        private IEP_Model db = new IEP_Model();
        public readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Server Hub
        public void Send(long IDAuc, string lastBidderEmail)
        {

            lock (mutex.lockObject)
            {

                AspNetUser user = db.AspNetUsers.SingleOrDefault(r => r.Email == lastBidderEmail);
                auction auction = db.auctions.SingleOrDefault(a => a.IDAuc == IDAuc);
                long productNewPrice = auction.price + auction.increment + 1;

                // User can bid!
                if ((productNewPrice <= user.TokenNumber))
                {
                    long newBidderCount = user.TokenNumber - productNewPrice;
                    user.TokenNumber = newBidderCount;
                    db.Entry(user).State = EntityState.Modified;

                    bid newBid = new bid();
                    newBid.Id = user.Id;
                    newBid.IDAuc = auction.IDAuc;
                    newBid.bidTime = DateTime.Now;
                    newBid.tokens = productNewPrice;
                    db.bids.Add(newBid);

                    // Return tokens to previous client
                    var previousBidder =    from o
                                            in db.UserAuctionInvested
                                            where o.AuctionId == auction.IDAuc
                                            select o.UserId;

                    string previousBidderID = previousBidder.SingleOrDefault();
                    AspNetUser previous = db.AspNetUsers.Find(previousBidderID);

                    // Previous bidder exists
                    if (previousBidderID != null)
                    {
                        long tokensToReturn = (long)db.UserAuctionInvested.Where(u => u.AuctionId == auction.IDAuc && u.UserId == previousBidderID).SingleOrDefault().TokenInvested;
                        long newTokenCount = previous.TokenNumber + tokensToReturn;
                        previous.TokenNumber = newTokenCount;

                        // PUSH NOTIFICATION: SET PREVIOUS CLIENT'S TOKEN NUMBER
                        var clientSelector = "token" + previous.Email.Replace("@", "_");
                        var clientAlertSelector = "alertToken" + previous.Email.Replace("@", "_");
                        var warningAlertSelector = "warning" + previous.Email.Replace("@", "_");
                        // PREVIOUS CLIENT IS ONLINE
                        if (hashUsersConnIds.ContainsKey(previous.Email) && previous.Email != lastBidderEmail)
                        {
                            Clients.Client(hashUsersConnIds[previous.Email]).setTokenNumber(clientSelector, newTokenCount, clientAlertSelector, warningAlertSelector, auction.product_name);
                        }
                        // PUSH NOTIFICATION: CURRENT CLIENT (LAST BIDDER) NEW TOKEN COUNT
                        clientSelector = "token" + lastBidderEmail.Replace("@", "_");
                        clientAlertSelector = "alertToken" + lastBidderEmail.Replace("@", "_");
                        warningAlertSelector = "warning" + previous.Email.Replace("@", "_");
                        if (previous.Email != lastBidderEmail && hashUsersConnIds.ContainsKey(lastBidderEmail)) {
                            Clients.Client(hashUsersConnIds[lastBidderEmail]).setTokenNumber(clientSelector, newBidderCount, clientAlertSelector, "x", "x");
                        }
                        db.Entry(previous).State = EntityState.Modified;
                        UserAuctionInvested uaiPrevious = db.UserAuctionInvested.Find(previousBidderID, auction.IDAuc);
                        db.UserAuctionInvested.Remove(uaiPrevious);
                    } 
                    // This client is first bidder
                    else
                    {
                        var clientSelector = "token" + lastBidderEmail.Replace("@", "_");
                        var clientAlertSelector = "alertToken" + lastBidderEmail.Replace("@", "_");
                        // PUSH NOTIFICATION: CURRENT CLIENT (LAST BIDDER) NEW TOKEN COUNT
                        if (hashUsersConnIds.ContainsKey(lastBidderEmail))
                        {
                            Clients.Client(hashUsersConnIds[lastBidderEmail]).setTokenNumber(clientSelector, newBidderCount, clientAlertSelector, "x", "x");
                        }
                    }

                    // Creating new bid userAuctionInvested -> [dbo].[userAuctionInvested] (user, auction, tokenNo)
                    UserAuctionInvested uai = new UserAuctionInvested();
                    uai.AuctionId = auction.IDAuc;
                    uai.UserId = user.Id;
                    uai.TokenInvested = productNewPrice;
                    db.UserAuctionInvested.Add(uai);

                    // Update Auction
                    auction.increment += 1;
                    auction.lastbidder = lastBidderEmail;

                    // Seconds to end of auction
                    var secondsDifference = ((DateTime)auction.close_date_time - DateTime.Now).TotalSeconds;
                    if (secondsDifference <= 10)
                    {
                        DateTime oldCloseDateTime = (DateTime)auction.close_date_time;
                        DateTime newCloseDateTime = oldCloseDateTime.AddSeconds(10);
                        auction.close_date_time = newCloseDateTime;
                        auction.duration += 10;
                    }

                    db.Entry(auction).State = EntityState.Modified;
                    string remainingToEnd = ((DateTime)auction.close_date_time - DateTime.Now).ToString(@"dd\:hh\:mm\:ss");
                    Clients.All.clientBidsUpdate(IDAuc, auction.state, remainingToEnd, lastBidderEmail, auction.price + auction.increment, "false");
                    // Update details auction page // clientWarningSelector, auctionNameWarning
                    Clients.All.auctionDetailsUpdate(IDAuc, lastBidderEmail, auction.price + auction.increment, newBid.bidTime.ToString(@"dd\:hh\:mm\:ss"), "Open");
                    db.SaveChanges();
                    return;
                    
                }
                // Client was previous bidder needs to pay +1 on actual price
                else if (auction.lastbidder == user.Email)
                {
                    if (user.TokenNumber > 0) // can place next bid
                    {
                        user.TokenNumber = user.TokenNumber - 1;
                        db.Entry(user).State = EntityState.Modified;
                        bid newBid = new bid();
                        newBid.Id = user.Id;
                        newBid.IDAuc = auction.IDAuc;
                        newBid.bidTime = DateTime.Now;
                        newBid.tokens = auction.price + auction.increment + 1;
                        db.bids.Add(newBid);
                        if (hashUsersConnIds.ContainsKey(lastBidderEmail))
                        {
                            var clientSelector = "token" + lastBidderEmail.Replace("@", "_");
                            var clientAlertSelector = "alertToken" + lastBidderEmail.Replace("@", "_");
                            var clientWarningSelector = "warning" + lastBidderEmail.Replace("@", "_");
                            Clients.Client(hashUsersConnIds[lastBidderEmail]).setTokenNumber(clientSelector, user.TokenNumber, clientAlertSelector, "x", "x");
                        }
                        // Updating userAuctionInvested -> [dbo].[userAuctionInvested] (user, auction, tokenNo)
                        UserAuctionInvested uai = db.UserAuctionInvested.Where(u => u.AuctionId == auction.IDAuc && u.UserId == user.Id).SingleOrDefault();
                        uai.TokenInvested += 1;
                        db.Entry(uai).State = EntityState.Modified;
                        // Update Auction
                        auction.increment += 1;
                        // Seconds to end of auction
                        var secondsDifference = ((DateTime)auction.close_date_time - DateTime.Now).TotalSeconds;
                        if (secondsDifference <= 10)
                        {
                            DateTime oldCloseDateTime = (DateTime)auction.close_date_time;
                            DateTime newCloseDateTime = oldCloseDateTime.AddSeconds(10);
                            auction.close_date_time = newCloseDateTime;
                            auction.duration += 10;
                        }
                        db.Entry(auction).State = EntityState.Modified;
                        string remainingToEnd = ((DateTime)auction.close_date_time - DateTime.Now).ToString(@"dd\:hh\:mm\:ss");
                        Clients.All.clientBidsUpdate(IDAuc, auction.state, remainingToEnd, lastBidderEmail, auction.price + auction.increment, "false");
                        Clients.All.auctionDetailsUpdate(IDAuc, lastBidderEmail, auction.price + auction.increment, newBid.bidTime.ToString(@"dd\:hh\:mm\:ss"), "Open");

                        db.SaveChanges();
                        return;
                    }
                }
                // No tokens - Warn user!
                string remaining = ((DateTime)auction.close_date_time - DateTime.Now).ToString(@"dd\:hh\:mm\:ss");
                Clients.All.clientBidsUpdate(IDAuc, auction.state, remaining, lastBidderEmail, auction.price + auction.increment, "true");
            }
        }

        // Registring client
        public void registerConId(string email)
        {
            hashUsersConnIds[email] = Context.ConnectionId;
        }

    }

    public class MyRegistry : Registry
    {

        public readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MyRegistry()
        {
            
            Schedule(() =>
            {

                // Check if some auction is over each 1 sec
                lock (mutex.lockObject)
                {

                    IEP_Model db = new IEP_Model();
                    var hubContext = GlobalHost.ConnectionManager.GetHubContext<MyHub>();

                    var auctions = from o 
                                   in db.auctions
                                   where o.state == "Open"
                                   select o;
                    
                    var auctionsList = auctions.ToList();

                    foreach (var auction in auctionsList)
                    {

                        DateTime now = DateTime.Now;
                        DateTime end = (DateTime)auction.close_date_time;

                        // Time is up! Auction is "Expired" or "Sold"
                        if (now >= end)
                        {
                            var edited = db.auctions.Find(auction.IDAuc);
                            // NO Winner
                            if (edited.increment == 0)
                            {
                                edited.state = "Expired";
                                db.Entry(edited).State = EntityState.Modified;
                                TimeSpan timeEnd = new TimeSpan(0);
                                string newDurationExpired = timeEnd.ToString(@"dd\:hh\:mm\:ss");

                                hubContext.Clients.All.timerUpdate(auction.IDAuc, edited.state, newDurationExpired, " - ", edited.price, "false", "odd", "true");
                                db.SaveChanges();
                            }
                            // YES Winner
                            else
                            {
                                edited.state = "Sold";
                                db.Entry(edited).State = EntityState.Modified;
                                db.SaveChanges();

                                // Refresh client
                                long soldPrice = edited.price + edited.increment;
                                TimeSpan timeEnd = new TimeSpan(0);
                                string newDurationSold = timeEnd.ToString(@"dd\:hh\:mm\:ss");
                                hubContext.Clients.All.timerUpdate(auction.IDAuc, edited.state, newDurationSold, edited.lastbidder, soldPrice, "false", "odd", "true");

                                // Return tokens to non-winners
                                string winnerId = db.AspNetUsers.Where(a => a.Email == edited.lastbidder).SingleOrDefault().Id;
                                //var bids = db.bids.Where(b => b.IDAuc == auction.IDAuc && b.Id != winnerId).ToList();
                                var userInvested = db.UserAuctionInvested.Where(i => i.AuctionId == auction.IDAuc);

                                foreach (var item in userInvested.ToList())
                                {
                                    if (item.UserId != winnerId) // Not winner - return money
                                    {
                                        AspNetUser FetchUser = db.AspNetUsers.Find(item.UserId);
                                        FetchUser.TokenNumber += (long)item.TokenInvested;
                                        db.Entry(FetchUser).State = EntityState.Modified;
                                    }
                                    db.UserAuctionInvested.Remove(item);
                                }
                                
                                db.SaveChanges();

                                // Send Email to winner
                                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
                                //String emailAddress = edited.lastbidder;
                                string emailAddress = "lukadragic032@gmail.com";
                                message.From = new MailAddress("iepaukcija@mail.com");
                                message.To.Add(new MailAddress(emailAddress));
                                message.IsBodyHtml = true;
                                message.BodyEncoding = Encoding.UTF8;
                                message.Subject = "IEP Auction: Congratulations! You have successfuly bought " + edited.product_name + ".";
                                long endingPrice = edited.price + edited.increment;
                                message.Body = "You have successfully took part in auction!" + "<br/>" +
                                                "---------------------------------------------------------" + "<br/>" +
                                                "Product name:\t\t" + edited.product_name + "<br/>" +
                                                "Started on:\t\t" + edited.open_date_time + "<br/>" +
                                                "Duration:\t\t" + edited.duration + "<br/>" +
                                                "Final price:\t\t" + endingPrice + "  ( " + edited.price + " )" + "<br/>" +
                                                "---------------------------------------------------------" + "<br/>" +
                                                "Your http://iepaukcija.azurewebsites.net/";
                                SmtpClient client = new SmtpClient();
                                try {
                                    client.Send(message);
                                }
                                catch(Exception e) {
                                    logger.Error("MyRegistry // Schedule() // Email sending to winner " + e.ToString());
                                }
                            }
                        }

                        // Update auction - is still active
                        long actualPrice = auction.price + auction.increment;
                        TimeSpan difference = end.Subtract(now);
                        string newDuration = difference.ToString(@"dd\:hh\:mm\:ss");

                        string lastTenSeconds__ = "false";
                        string numberOddEven__ = "odd";
                        string end__ = "false";
                        if (difference.TotalSeconds <= 10)
                        {
                            lastTenSeconds__ = "true";
                            if (difference.TotalSeconds <= 1)
                            {
                                end__ = "true";
                            }
                            if ((int)Math.Ceiling(difference.TotalSeconds) % 2 == 0) 
                            {
                                numberOddEven__ = "even";
                            }
                        }
                        hubContext.Clients.All.timerUpdate(auction.IDAuc, auction.state, newDuration, auction.lastbidder, actualPrice, lastTenSeconds__, numberOddEven__, end__);
                    }

                }
            }).ToRunNow().AndEvery(1).Seconds();
        }

    }

}