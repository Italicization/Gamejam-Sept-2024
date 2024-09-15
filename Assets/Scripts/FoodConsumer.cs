using System;

public class FoodConsumer : SizedObject
{
	public bool CanEat(FoodItem food) => food.Size < Size;
	
	public bool Eat(FoodItem food)
	{
		if (!CanEat(food))
			return false;
		
		float foodVolume = food.Size * food.Size * food.Size;
		float consumerVolume = Size * Size * Size;
		float sizeChange = Size - (float)Math.Cbrt(consumerVolume + foodVolume);
		Grow(sizeChange);
		food.Eat();
		return true;
	}
}
