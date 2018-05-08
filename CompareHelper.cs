using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Tools
{
    public class CompareHelper
    {
        public static bool CompareObjects(object inputObjectA, object inputObjectB, string[] ignorePropertiesList)
        {
            var areObjectsEqual = true;
            //check if both objects are not null before starting comparing children
            if (inputObjectA != null && inputObjectB != null)
            {
                var properties =
                    inputObjectA.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                //get all public properties of the object using reflection   
                foreach (var propertyInfo in properties)
                {
                    //if it is not a readable property or if it is a ignorable property
                    //ingore it and move on
                    if (!propertyInfo.CanRead || ignorePropertiesList.Contains(propertyInfo.Name))
                        continue;

                    //get the property values of both the objects
                    var value1 = propertyInfo.GetValue(inputObjectA, null);
                    var value2 = propertyInfo.GetValue(inputObjectB, null);

                    // if the objects are primitive types such as (integer, string etc)
                    //that implement IComparable, we can just directly try and compare the value      
                    if (IsAssignableFrom(propertyInfo.PropertyType) || IsPrimitiveType(propertyInfo.PropertyType) ||
                        IsValueType(propertyInfo.PropertyType))
                    {
                        //compare the values
                        if (!CompareValues(value1, value2))
                        {
                            Console.WriteLine("PropertyType {0} Property Name {1} value 1 {2} value 2 {3}", propertyInfo.PropertyType, propertyInfo.Name, value1, value2);
                            areObjectsEqual = false;
                        }
                    }
                    //if the property is a collection (or something that implements IEnumerable)
                    //we have to iterate through all items and compare values
                    else if (IsEnumerableType(propertyInfo.PropertyType))
                    {
                        //Console.WriteLine("Property Name {0}", propertyInfo.Name);
                        if (CompareEnumerations(value1, value2, ignorePropertiesList)) continue;
                        Console.WriteLine("Compare differents PropertyType {0} Property Name {1} value 1 {2} value 2 {3}", propertyInfo.PropertyType, propertyInfo.Name, value1, value2);
                        areObjectsEqual = false;
                    }
                    //if it is a class object, call the same function recursively again
                    else if (propertyInfo.PropertyType.IsClass)
                    {
                        if (!CompareObjects(propertyInfo.GetValue(inputObjectA, null),
                            propertyInfo.GetValue(inputObjectB, null), ignorePropertiesList))
                        {
                            areObjectsEqual = false;
                        }
                    }
                    else
                    {
                        areObjectsEqual = false;
                    }
                }
            }
            else if (!(inputObjectA == null && inputObjectB == null))//Both null -> equals
            {
                areObjectsEqual = false;
            }

            return areObjectsEqual;
        }

        //true if c and the current Type represent the same type, or if the current Type is in the inheritance 
        //hierarchy of c, or if the current Type is an interface that c implements, 
        //or if c is a generic type parameter and the current Type represents one of the constraints of c. false if none of these conditions are true, or if c is null.
        private static bool IsAssignableFrom(Type type)
        {
            return typeof(IComparable).IsAssignableFrom(type);
        }

        private static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive;
        }

        private static bool IsValueType(Type type)
        {
            return type.IsValueType;
        }

        private static bool IsEnumerableType(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        /// <summary>
        /// Compares two values and returns if they are the same.
        /// </summary>        
        private static bool CompareValues(object value1, object value2)
        {
            var areValuesEqual = true;
            var selfValueComparer = value1 as IComparable;

            // one of the values is null             
            if (value1 == null && value2 != null || value1 != null && value2 == null)
                areValuesEqual = false;
            else if (selfValueComparer != null && selfValueComparer.CompareTo(value2) != 0)
                areValuesEqual = false;
            else if (!Equals(value1, value2))
                areValuesEqual = false;

            return areValuesEqual;
        }

        private static bool CompareEnumerations(object value1, object value2, string[] ignorePropertiesList)
        {
            // if one of the values is null, no need to proceed return false;
            if ((value1 == null && value2 != null) || (value1 != null && value2 == null))
                return false;
            if (value1 == null /*&& value2 == null*/) return true;// no need to check value2 == null

            var enumValue1 = ((IEnumerable) value1).Cast<object>();
            var enumValue2 = ((IEnumerable) value2).Cast<object>();

            // if the items count are different return false
            var objects1 = enumValue1.ToList();
            var objects2 = enumValue2.ToList();
            if (objects1.Count() != objects2.Count())
                return false;

            // if the count is same, compare individual item 
            for (var itemIndex = 0; itemIndex < objects1.Count(); itemIndex++)
            {
                var enumValue1Item = objects1.ElementAt(itemIndex);
                var enumValue2Item = objects2.ElementAt(itemIndex);
                var enumValue1ItemType = enumValue1Item.GetType();
                if (IsAssignableFrom(enumValue1ItemType) || IsPrimitiveType(enumValue1ItemType) ||
                    IsValueType(enumValue1ItemType))
                {
                    if (CompareValues(enumValue1Item, enumValue2Item)) continue;
                    Console.WriteLine("Compare different at index {0} value 1 {1} value 2 {2}", itemIndex, enumValue1Item,
                        enumValue2Item);
                    return false;
                }

                if (!CompareObjects(enumValue1Item, enumValue2Item, ignorePropertiesList))
                {
                    return false;
                }
            }

            return true;
        }
    }
}