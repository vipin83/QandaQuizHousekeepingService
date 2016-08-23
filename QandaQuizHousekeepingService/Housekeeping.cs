using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ef = QandaQuizEntityFramework;

namespace QandaQuizHousekeepingService
{
    public static class Housekeeping
    {

        public static ef.QandaQuizContext dbContext = new ef.QandaQuizContext();
        
        public static void runMundaneTasks()
        {
            //run this peice of code in a timely fashion

            CreateLogEntry("Start");
          
            var currentActiveQuizDetails = dbContext.quizDetails
                                                    .Where(qd => !qd.quizLapsed && qd.quizCurrentlyActive)
                                                    .FirstOrDefault();


            //1. check if active date time of current quiz is past the next quiz active date time. if so then lapse first quiz and make next quiz state as active
            if (currentActiveQuizDetails != null)
            {

                CreateLogEntry("currently active quiz id# " + currentActiveQuizDetails.Id + " active from " + currentActiveQuizDetails.quizAvtiveDateTime);
                
                ef.QuizDetail nextToBeActiveQuizDetails = dbContext.quizDetails
                                                           .Where(qd => !qd.quizLapsed && qd.quizAvtiveDateTime > currentActiveQuizDetails.quizAvtiveDateTime)
                                                           .OrderBy(o => o.quizAvtiveDateTime)
                                                           .FirstOrDefault();


                if (nextToBeActiveQuizDetails != null) //expire current quiz only if there is any other quiz in the system which can be made active
                {
                    CreateLogEntry("next to-be active quiz id# " + nextToBeActiveQuizDetails.Id + " active from " + nextToBeActiveQuizDetails.quizAvtiveDateTime);

                    //check if current time is past the next ToBe active quiz datetime
                    if (DateTime.Now >= nextToBeActiveQuizDetails.quizAvtiveDateTime)
                    {
                        CreateLogEntry("expiring current quiz");

                        currentActiveQuizDetails.quizLapsed = true;
                        currentActiveQuizDetails.quizCurrentlyActive = false;

                        nextToBeActiveQuizDetails.quizLapsed = false;
                        nextToBeActiveQuizDetails.quizCurrentlyActive = true;
                        
                        dbContext.SaveChanges();
                    }
                }
            }

            //2. check if there is any lapsed quiz which does not have winner
            var lapsedQuizNoWinner = dbContext.quizDetails
                                              .Where(qd => qd.quizLapsed && String.IsNullOrEmpty(qd.quizWinnerId))
                                              .OrderBy(o => o.quizAvtiveDateTime)
                                              .ToList();

            foreach (var lapsedQuiz in lapsedQuizNoWinner)
            {

                var correctEntriesForQuiz = dbContext.quizPlayDetails
                                                     .Where(p => p.QuizDetail.Id == lapsedQuiz.Id &&
                                                                 p.QuizAnswer.Id == p.QuizDetail.QuizQuestion.QuizAnswers.Where(q => q.quizAnswerCorrect).FirstOrDefault().Id &&
                                                                 !p.IsThisFreePlay
                                                           );

                var correctEntriesCountForQuiz = correctEntriesForQuiz.Count();

                //(announce winner only if it has receievd sufficient 'correct' responses? )
                if (correctEntriesCountForQuiz >= lapsedQuiz.quizTimesNumberOfEntriesAllowed)
                {
                    //3. announce winner
                    var quizWinnerNumber = lapsedQuiz.quizWinnerNumber;

                    //sort quiz answer entries in the order they were answered
                    var quizWinnerEntry = correctEntriesForQuiz.OrderBy(o => o.quizSubmittedDateTime)
                                                               .Skip(quizWinnerNumber - 1)
                                                               .FirstOrDefault();

                    //update lapsed quiz with the winner number
                    lapsedQuiz.quizWinnerId = quizWinnerEntry.user_Id;
                    dbContext.SaveChanges();

                    //4. Send email to winner + site owner
                    var emailMgr = new clsEmail();

                    try
                    {
                        emailMgr.SendEmailUsingSMTP(quizWinnerEntry.AspNetUser.Email,
                            "admin@QandAQuiz.com",
                            "Congratulations! you are the winner of QandA Quiz!!",
                            "You have won the quiz with prize money of " + lapsedQuiz.quizPrizeMoney.ToString("C"));

                        emailMgr.SendEmailUsingSMTP("admin@QandAQuiz.com",
                            "admin@QandAQuiz.com",
                            "Quiz # " + lapsedQuiz.Id + "(" + lapsedQuiz.quizTitle + ") winner announced",
                            quizWinnerEntry.AspNetUser.Email + " has been declared winner of quiz# " + lapsedQuiz.Id + "(" + lapsedQuiz.quizTitle + ")");
                    }
                    catch
                    { }
                }

            }

            dbContext.LogEntries.Add(new ef.LogEntry
            {
                LogEntryDateTime = DateTime.Now,
                LogEntryDescription = "Housekeeping Service - Housekeeping - End"
            });

            dbContext.SaveChanges();
        }
        
        private static void CreateLogEntry(string message)
        {

            dbContext.LogEntries.Add(new ef.LogEntry
            {
                LogEntryDateTime = DateTime.Now,
                LogEntryDescription = "Housekeeping Service - " + message
            });

            dbContext.SaveChanges();

        }

    }
}