using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("DestroyEffectArray", "Destroys an array of effects.", CustomMethodType.Action)]
    class DestroyEffectArray : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("effectArray", "The array of effects."),
                new ConstNumberParameter("destroyPerLoop", "The number of effects to destroy per iteration.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            Element effectArray = (Element)parameterValues[0];
            double destroyPerLoop = (double)additionalParameterData[1];

            ForeachBuilder foreachBuilder = new ForeachBuilder(actionSet, effectArray);
            foreachBuilder.Setup();

            for (int i = 0; i < destroyPerLoop; i++)
            {
                if (i == 0)
                    actionSet.AddAction(
                        Element.Part<A_DestroyEffect>(foreachBuilder.IndexValue)
                    );
                else
                    actionSet.AddAction(
                        Element.Part<A_DestroyEffect>(Element.Part<V_ValueInArray>(effectArray, foreachBuilder.Index + i))
                    );
            }

            foreachBuilder.Finish();

            return null;
        }
    }
}