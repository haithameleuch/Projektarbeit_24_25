using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    [System.Serializable]
    public class Question
    {
        public string text;
        public int answer;

        public Question(string text, int answer)
        {
            this.text = text;
            this.answer = answer;
        }
    }

    public class QuestionManager : MonoBehaviour
    {
        public List<Question> questions = new List<Question>();
        private Question _currentQuestion;

        void Start()
        {
            LoadQuestions();
            AskRandomQuestion();
        }

        // Load your questions here
        public void LoadQuestions()
        {
            questions.Add(new Question("Ein Raum hat vier Ecken. In jeder Ecke sitzt eine Katze. Vor jeder Katze sitzen drei Katzen. Wie viele Katzen sind im Raum?", 4));
            questions.Add(new Question("Ein Magier besitzt 3 Zauberstäbe. Jeder Zauberstab hat 2 Kristalle. Wie viele Kristalle besitzt der Magier insgesamt?", 6));
            questions.Add(new Question("Der Runenkreis zeigt die Zahlen: 2, 4, 6, ?. Welche Zahl folgt logisch?", 8));
            questions.Add(new Question("Ein Uhrturm schlägt alle 3 Stunden. Wie oft schlägt er in 24 Stunden?", 8));
            questions.Add(new Question("Wie viele Beine hat eine Spinne minus die Anzahl der Buchstaben im Wort 'Spinne'?", 2)); // 8 - 6 = 2
            questions.Add(new Question("Ein Zaubertrank benötigt 9 Tropfen. Zwei Tropfen verdampfen. Wie viele bleiben übrig?", 7));
            questions.Add(new Question("Du siehst drei Spiegel. Jeder Spiegel zeigt dich zweimal. Wie viele Spiegelbilder siehst du?", 6));
            questions.Add(new Question("Wie viele Buchstaben hat das Wort 'Feuer'?", 5));
            questions.Add(new Question("Ein altes Schloss hat 3 Riegel. Jeder Riegel kann offen (1) oder zu (0) sein. Wie viele Kombinationen gibt es?", 8));
            questions.Add(new Question("Wie viele Vokale sind im Wort 'Magie'?", 3));
            questions.Add(new Question("IX steht an der Wand. Wandle es in eine Ziffer um.", 9));
            questions.Add(new Question("Ein Drache hat 9 Köpfe. Du schlägst 2 ab. Für jeden abgeschlagenen wachsen 1 neue nach. Wie viele Köpfe hat er jetzt?", 9));
            questions.Add(new Question("Du würfelst zwei Würfel. Einer zeigt 2, der andere 3. Was ist die Summe?", 5));
            questions.Add(new Question("Wie viele Finger hat eine einzelne menschliche Hand?", 5));
            questions.Add(new Question("Der Zauberlehrling zählt die Monde: Neu, Halb, Voll. Wie viele Phasen sind es?", 4));
            questions.Add(new Question("Zähle die Buchstaben im Wort 'Wasser'.", 6));
            questions.Add(new Question("Ein Rätsel stellt: 'Ich bin kleiner als 5, aber größer als 1. Ich bin ungerade.' Was bin ich?", 3));
            questions.Add(new Question("Wie viele Elemente siehst du: Feuer, Wasser, Erde, Luft?", 4));
            questions.Add(new Question("Ein Kobold stellt drei Fragen. Du beantwortest zwei falsch. Wie viele richtig?", 1));
            questions.Add(new Question("'Ich bin eine Zahl, die durch 3 teilbar ist und kleiner als 1.' Welche ganze Zahl bin ich?", 0));
        }

        // Ask a random question from the list
        public void AskRandomQuestion()
        {
            if (questions.Count == 0) return;
            _currentQuestion = questions[Random.Range(0, questions.Count)];
            Debug.Log("Question: " + _currentQuestion.text);
        }

        // Check if user's predicted digit (from classifier) is correct
        public bool CheckAnswer(int predictedDigit)
        {
            bool correct = predictedDigit == _currentQuestion.answer;
            Debug.Log(correct ? "Correct!" : $"Wrong! Expected {_currentQuestion.answer}");
            return correct;
        }

        // Get current question (for UI display, etc.)
        public string GetCurrentQuestionText()
        {
            return _currentQuestion != null ? _currentQuestion.text : "";
        }
    }
}