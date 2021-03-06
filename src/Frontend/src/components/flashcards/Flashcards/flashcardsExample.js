const flashcardsExample = [
	convertToFlashcard(
		'Что такое полиморфизм и зачем он нужен?',
		'Полиморфизм позволяет объектам производного класса обрабатываться как объектам базового класса или интерфейса.' +
		' Таким образом, алгоритмы использующие в работе некоторый интерфейс могут быть применены ко всем классам-наследниками.' +
		' При этом виртуальные методы и реализация интерфейса в каждом наследнике может быть различной.'
		, 'первое знакомство'),
	convertToFlashcard(
		'Что такое класс?',
		'Это элемент ПО, описывающий абстрактный тип данных и его частичную или полную реализацию',
		'основы программирования 1'),
	convertToFlashcard(
		'Что такое ООП?',
		'Методология программирования, основанная на представлении программы в виде совокупности объектов,' +
		' каждый из которых является экземпляром определённого класса, а классы образуют иерархию наследования',
		'основы программирования 1'
	),
	convertToFlashcard()
];

let idCounter = 0;

function convertToFlashcard(question = 'вопрос', answer = 'ответ', unitTitle = 'тема', rate = 'notRated') {
	idCounter++;
	return {
		id: idCounter.toString(),
		question: question,
		answer: answer,
		unitTitle: unitTitle,
		rate: rate,
		unitId: idCounter.toString()
	}
}

export default flashcardsExample;
