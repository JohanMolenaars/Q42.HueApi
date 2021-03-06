﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Q42.HueApi.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Q42.HueApi.Models
{
  [DataContract]
  public class Schedule
  {
    [IgnoreDataMember]
    public string Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "command")]
    public InternalBridgeCommand Command { get; set; }

    /// <summary>
    /// UTC time
    /// </summary>
    [DataMember(Name = "time")]
		[JsonConverter(typeof(HueDateTimeConverter))]
    [Obsolete("Use the LocalTime property. This property will be removed in the future.")]
		public HueDateTime Time { get; set; }

    [DataMember(Name = "localtime")]
    [JsonConverter(typeof(HueDateTimeConverter))]
    public HueDateTime LocalTime { get; set; }
    
    [DataMember(Name = "created")]
    public DateTime? Created { get; set; }

    /// <summary>
    /// UTC time that the timer was started. Only provided for timers.
    /// </summary>
    [DataMember(Name = "starttime")]
    public DateTime? StartTime { get; set; }

    //TODO: Create Enum with enabled and disabled option
    /// <summary>
    /// "enabled"  Schedule is enabled
    /// "disabled"  Schedule is disabled by user.
    /// Application is only allowed to set “enabled” or “disabled”. Disabled causes a timer to reset when activated (i.e. stop & reset). “enabled” when not provided on creation.
    /// </summary>
    [DataMember(Name = "status")]
    public string Status { get; set; }

    /// <summary>
    /// If set to true, the schedule will be removed automatically if expired, if set to false it will be disabled. Default is true
    /// </summary>
    [DataMember(Name = "autodelete")]
    public bool? AutoDelete { get; set; }

  }

}
