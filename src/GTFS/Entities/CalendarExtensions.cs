﻿// The MIT License (MIT)

// Copyright (c) 2015 Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace GTFS.Entities
{
    /// <summary>
    /// Contains extension methods related to the calendar.
    /// </summary>
    public static class CalendarExtensions
    {
        /// <summary>
        /// Returns true if the calendar covers the given date.
        /// </summary>
        /// <returns></returns>
        public static bool CoversDate(this Calendar calendar, DateTime date)
        {
            date = date.Date;
            if (calendar.StartDate <= date && calendar.EndDate >= date)
            { // ok in range.
                return calendar[date.DayOfWeek];
            }
            return false;
        }

        /// <summary>
        /// Gets the status for the given date, don't care, true or false.
        /// </summary>
        /// <returns></returns>
        public static bool? GetStatusFor(this Calendar calendar, DateTime date)
        {
            date = date.Date;
            if (calendar.StartDate <= date && calendar.EndDate >= date)
            { // ok in range.
                return calendar[date.DayOfWeek];
            }
            return null;
        }

        /// <summary>
        /// Adds or subtracts the given calendar date and returns one or more new calendar entities representing the same data.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Calendar> AddOrSubtract(this Calendar calendar, CalendarDate calendarDate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a calendar entity for the day.
        /// </summary>
        /// <returns></returns>
        public static Calendar CreateCalendar(this DateTime day, string serviceId)
        {
            var calendar = new Calendar()
            {
                StartDate = day,
                EndDate = day,
                ServiceId = serviceId
            };
            calendar[day.DayOfWeek] = true;
            return calendar;
        }

        /// <summary>
        /// Adds the given calendar day and returns one or more new calendar entities representing the new data.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Calendar> Add(this Calendar calendar, DateTime day)
        {
            if(calendar.CoversDate(day))
            { // no work needs to be done.
                return new Calendar[] { calendar };
            }

            // naively add another calendar entity representing one day.
            if ((calendar.EndDate - calendar.StartDate).Days <= 7 &&
                calendar.StartDate <= day &&
                calendar.EndDate >= day)
            {
                var newCalendar = new Calendar()
                {
                    EndDate = calendar.EndDate,
                    StartDate = calendar.StartDate,
                    Mask = calendar.Mask,
                    ServiceId = calendar.ServiceId
                };
                newCalendar.Set(day, true);
                return new Calendar[] { newCalendar };
            }
            return new Calendar[] { calendar, day.CreateCalendar(calendar.ServiceId) };
        }

        /// <summary>
        /// Subtracts the given calendar day and returns one or more new calendar entities representing the same data.
        /// </summary>
        public static IEnumerable<Calendar> Subtract(this Calendar calendar, DateTime day)
        {
            if (!calendar.CoversDate(day))
            { // no work needs to be done.
                return new Calendar[] { calendar };
            }

            var firstDayOfWeek = day.FirstDayOfWeek();
            var lastDayOfWeek = day.LastDayOfWeek();

            if(firstDayOfWeek <= calendar.StartDate &&
               lastDayOfWeek >= calendar.EndDate)
            { // yay! this is exceptional but let's take advantage of this.
                calendar[day.DayOfWeek] = false;
                return new Calendar[] { calendar };
            }

            // possibly split in two or three pieces.
            if(firstDayOfWeek <= calendar.StartDate)
            { // two pieces, a first week with the day substracted and the rest.
                var subtracted = new Calendar()
                {
                    StartDate = calendar.StartDate,
                    EndDate = lastDayOfWeek
                };
                subtracted.CopyWeekPatternFrom(calendar);
                subtracted[day.DayOfWeek] = false;

                var rest = new Calendar()
                {
                    StartDate = lastDayOfWeek.AddDays(1),
                    EndDate = calendar.EndDate
                };
                rest.CopyWeekPatternFrom(calendar);
                return new Calendar[] { subtracted, rest };
            }
            else if (lastDayOfWeek >= calendar.EndDate)
            { // two pieces, a last week with the day substracted and the rest.
                var rest = new Calendar()
                {
                    StartDate = calendar.StartDate,
                    EndDate = firstDayOfWeek.AddDays(-1)
                };
                rest.CopyWeekPatternFrom(calendar);

                var subtracted = new Calendar()
                {
                    StartDate = firstDayOfWeek,
                    EndDate = calendar.EndDate
                };
                subtracted.CopyWeekPatternFrom(calendar);
                subtracted[day.DayOfWeek] = false;
                return new Calendar[] { rest, subtracted };
            }
            else
            { // three pieces, a first period, a week with the day subtracted and a last period.
                var rest1 = new Calendar()
                {
                    StartDate = calendar.StartDate,
                    EndDate = firstDayOfWeek.AddDays(-1)
                };
                rest1.CopyWeekPatternFrom(calendar);

                var subtracted = new Calendar()
                {
                    StartDate = firstDayOfWeek,
                    EndDate = lastDayOfWeek
                };
                subtracted.CopyWeekPatternFrom(calendar);
                subtracted[day.DayOfWeek] = false;

                var rest2 = new Calendar()
                {
                    StartDate = lastDayOfWeek.AddDays(1),
                    EndDate = calendar.EndDate
                };
                rest2.CopyWeekPatternFrom(calendar);
                return new Calendar[] { rest1, subtracted, rest2 };
            }
        }

        /// <summary>
        /// Sets the mask for the given day. 
        /// </summary>
        public static void Set(this Calendar calendar, DateTime day, bool value)
        {
            if((calendar.EndDate - calendar.StartDate).Days > 7)
            {
                throw new InvalidOperationException("Cannot set mask for a specific day if the calendar object spans multiple weeks.");
            }

            if(calendar.StartDate > day ||
               calendar.EndDate < day)
            {
                throw new InvalidOperationException("Cannot set mask for a specific day if the day is not within the calendar's range.");
            }

            calendar[day.DayOfWeek] = value;
        }

        /// <summary>
        /// Copies the week pattern from the given calendar.
        /// </summary>
        public static void CopyWeekPatternFrom(this Calendar calendar, Calendar weekPatternSource)
        {
            calendar.Monday = weekPatternSource.Monday;
            calendar.Tuesday = weekPatternSource.Tuesday;
            calendar.Wednesday = weekPatternSource.Wednesday;
            calendar.Thursday = weekPatternSource.Thursday;
            calendar.Friday = weekPatternSource.Friday;
            calendar.Saturday = weekPatternSource.Saturday;
            calendar.Sunday = weekPatternSource.Sunday;
        }

        /// <summary>
        /// Gets the first day of the week that contains the given day.
        /// </summary>
        /// <remarks>First day of the week is monday, last day is sunday.</remarks>
        /// <returns></returns>
        public static DateTime FirstDayOfWeek(this DateTime day)
        {
            switch(day.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return day;
                case DayOfWeek.Tuesday:
                    return day.AddDays(-1);
                case DayOfWeek.Wednesday:
                    return day.AddDays(-2);
                case DayOfWeek.Thursday:
                    return day.AddDays(-3);
                case DayOfWeek.Friday:
                    return day.AddDays(-4);
                case DayOfWeek.Saturday:
                    return day.AddDays(-5);
                case DayOfWeek.Sunday:
                    return day.AddDays(-6);
            }
            throw new ArgumentOutOfRangeException("Day is not a valid day of the week.");
        }

        /// <summary>
        /// Gets the last day of the week that contains the given day.
        /// </summary>
        /// <remarks>First day of the week is monday, last day is sunday.</remarks>
        /// <returns></returns>
        public static DateTime LastDayOfWeek(this DateTime day)
        {
            switch (day.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return day.AddDays(6);
                case DayOfWeek.Tuesday:
                    return day.AddDays(5);
                case DayOfWeek.Wednesday:
                    return day.AddDays(4);
                case DayOfWeek.Thursday:
                    return day.AddDays(3);
                case DayOfWeek.Friday:
                    return day.AddDays(2);
                case DayOfWeek.Saturday:
                    return day.AddDays(1);
                case DayOfWeek.Sunday:
                    return day;
            }
            throw new ArgumentOutOfRangeException("Day is not a valid day of the week.");
        }

        /// <summary>
        /// Trims the end and start dates.
        /// </summary>
        public static void TrimDates(this Calendar calendar)
        {
            calendar.TrimStartDate();
            calendar.TrimEndDate();
        }

        /// <summary>
        /// Trims the start date when it's too early and the mask has trailing zero's.
        /// </summary>
        public static void TrimStartDate(this Calendar calendar)
        {
            if (calendar.Mask == 0) { return; }
            while (!calendar[calendar.StartDate.DayOfWeek])
            {
                calendar.StartDate = calendar.StartDate.AddDays(1);
            }
        }

        /// <summary>
        /// Trims the end date when it's too late and the mask has trailing zero's.
        /// </summary>
        public static void TrimEndDate(this Calendar calendar)
        {
            if (calendar.Mask == 0) { return; }
            while(!calendar[calendar.EndDate.DayOfWeek])
            {
                calendar.EndDate = calendar.EndDate.AddDays(-1);
            }
        }

        /// <summary>
        /// Tries to merge two calendars together.
        /// </summary>
        /// <returns></returns>
        public static bool TryMerge(this Calendar calendar, Calendar other, out Calendar merge)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the mask for the given week. The mask will be adjusted when part of the week is outside of the calendar-range.
        /// </summary>
        /// <returns></returns>
        public static byte MaskForWeek(this Calendar calendar, DateTime day)
        {
            if (day.DayOfWeek != DayOfWeek.Monday) { throw new ArgumentOutOfRangeException("The given day is not a monday."); }

            var mask = calendar.Mask;
            if (day < calendar.StartDate)
            { // a part could still be in there.
                var diff = (int)(calendar.StartDate - day).Days;
                if(diff >= 7)
                { // out of range of calendar.
                    return 0;
                }
                var diffMask = ~(1 << diff) & mask;
                return (byte)(diffMask & mask);
            } // day >= startDate.
            else if(day > calendar.EndDate)
            { // out of range of calendar.
                return 0;
            } // day >= startDate and day <= calendar.EndDate.
            else if(day.AddDays(7) > calendar.EndDate)
            { // a part could still be in there.
                var diff = (int)(day.AddDays(7) - calendar.EndDate).Days;
                if (diff >= 7)
                { // out of range of calendar.
                    return 0;
                }
                var diffMask = ~((1 << diff) - 1) & mask;
                return (byte)(diffMask & mask);
            } // day >= startDay and day.AddDays(7) <= calendar.EndDate.
            return mask;
        }

        /// <summary>
        /// Returns the calendar entities in this list that covers the specified DayOfWeek. If the DateTime is specified it will also restrict the list of calendar's to cover the specified date.
        /// </summary>
        /// <param name="calendars"></param>
        /// <param name="dow"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<Calendar> GetServicesForDayOfWeek(this List<Calendar> calendars, DayOfWeek dow, DateTime dt = new DateTime())
        {
            var outList = new List<Calendar>();

            bool checkForDate = dt != new DateTime();

            foreach (var calendar in calendars)
            {
                if (calendar.ContainsDay(dow))
                {
                    if ((checkForDate && calendar.StartDate <= dt && calendar.EndDate >= dt) || !checkForDate)
                    {
                        outList.Add(calendar);
                    }
                }
            }
            return outList;
        }

        /// <summary>
        /// Returns true if this Calendar is true for the specified DayOfWeek.
        /// </summary>
        /// <param name="_self"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public static bool ContainsDay(this Calendar _self, DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Sunday:
                    return _self.Sunday;
                case DayOfWeek.Monday:
                    return _self.Monday;
                case DayOfWeek.Tuesday:
                    return _self.Tuesday;
                case DayOfWeek.Wednesday:
                    return _self.Wednesday;
                case DayOfWeek.Thursday:
                    return _self.Thursday;
                case DayOfWeek.Friday:
                    return _self.Friday;
                case DayOfWeek.Saturday:
                    return _self.Saturday;
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// Returns the calendars in this list that covers the specified date and its DayOfWeek.
        /// </summary>
        /// <param name="calendars"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<Calendar> GetServicesForDate(this List<Calendar> calendars, DateTime dt)
        {
            var outList = new List<Calendar>();
            foreach (var calendar in calendars)
            {
                if (calendar.CoversDate(dt) && calendar.ContainsDay(dt.DayOfWeek)) outList.Add(calendar);
            }
            return outList;
        }
    }
}