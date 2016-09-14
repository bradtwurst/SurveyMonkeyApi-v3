﻿using Newtonsoft.Json;

namespace SurveyMonkey.Containers.Enums
{
    [JsonConverter(typeof(LaxEnumJsonConverter))]
    public enum QuestionFamily
    {
        NotSet = 0,
        SingleChoice,
        MultipleChoice,
        Matrix,
        OpenEnded,
        Demographic,
        DateTime,
        Presentation,
        CustomVariable
    }
}