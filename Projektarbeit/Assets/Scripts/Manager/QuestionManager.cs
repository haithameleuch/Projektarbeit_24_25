using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Represents a single question with its text and correct answer.
    /// </summary>
    [System.Serializable]
    public class Question
    {
        /// <summary>
        /// The text of the question.
        /// </summary>
        public string text;
        
        /// <summary>
        /// The correct answer (as an integer) for the question.
        /// </summary>
        public int answer;

        /// <summary>
        /// Constructor to initialize a question with its text and answer.
        /// </summary>
        /// <param name="text">The question text.</param>
        /// <param name="answer">The correct answer.</param>
        public Question(string text, int answer)
        {
            this.text = text;
            this.answer = answer;
        }
    }

    /// <summary>
    /// Manages a list of questions, selects a random question,
    /// and checks player answers.
    /// </summary>
    public class QuestionManager : MonoBehaviour
    {
        /// <summary>
        /// List of all available questions.
        /// </summary>
        public List<Question> questions = new List<Question>();
        
        /// <summary>
        /// The currently active question.
        /// </summary>
        private Question _currentQuestion;

        /// <summary>
        /// Initialize the question manager, load questions, and pick a random question at start.
        /// </summary>
        public void Start()
        {
            LoadQuestions();
            AskRandomQuestion();
        }

        /// <summary>
        /// Loads the questions into the list.
        /// Can be extended to load from a file or database.
        /// </summary>
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
            
            // Haitham easy
            questions.Add(new Question("Wie viele Kontinente gibt es weltweit?", 7));
            questions.Add(new Question("Wie viele Farben hat die Flagge Deutschlands?", 3));
            questions.Add(new Question("Wie viele Tage hat eine Woche?", 7));
            questions.Add(new Question("Wie viele Ringe hat das olympische Symbol?", 5));
            questions.Add(new Question("Wie viele Seiten hat ein gewöhnlicher Spielwürfel?", 6));
            questions.Add(new Question("Wie viele Stunden vergehen bei einer halben Umdrehung des Uhrzeigers?", 6));
            questions.Add(new Question("Wie viele Ozeane unterscheidet man üblicherweise?", 5));
            questions.Add(new Question("Wie viele Länder grenzen an Deutschland?", 9));
            questions.Add(new Question("Wie viele Ecken hat ein Rechteck?", 4));
            questions.Add(new Question("Wie viele Sterne umfasst der Asterismus 'Großer Wagen'?", 7));
            
            // Haitham hard
            questions.Add(new Question("Wie viele Planeten unseres Sonnensystems besitzen ein Ringsystem?", 4));
            questions.Add(new Question("Durch wie viele Kontinente verläuft der Äquator?", 3));
            questions.Add(new Question("Wie viele Gehörknöchelchen befinden sich in einem menschlichen Ohr?", 3));
            questions.Add(new Question("Wie viele Minuten benötigt Sonnenlicht bis zur Erde (gerundet)?", 8));
            questions.Add(new Question("Wie viele allgemein anerkannte Grundgeschmacksrichtungen gibt es?", 5));
            questions.Add(new Question("Wie viele platonische Körper gibt es?", 5));
            questions.Add(new Question("Wie viele unterschiedliche Figurtypen hat jeder Spieler im Schach?", 6));
            questions.Add(new Question("Wie viele Monate im Jahr haben 31 Tage?", 7));
            questions.Add(new Question("Wie viele Planeten des Sonnensystems haben keine natürlichen Monde?", 2));
            questions.Add(new Question("Wie viele Planeten sind größer als die Erde?", 4));
        }

        /// <summary>
        /// Selects a random question from the question list.
        /// </summary>
        public void AskRandomQuestion()
        {
            if (questions.Count == 0) return;
            _currentQuestion = questions[Random.Range(0, questions.Count)];
            Debug.Log("Question: " + _currentQuestion.text+ "----> Answer:" + _currentQuestion.answer);
        }

        /// <summary>
        /// Checks if the predicted digit matches the current question's answer.
        /// </summary>
        /// <param name="predictedDigit">The player's predicted digit.</param>
        /// <returns>True if correct, false otherwise.</returns>
        public bool CheckAnswer(int predictedDigit)
        {
            var correct = predictedDigit == _currentQuestion.answer;
            Debug.Log(correct ? "Correct!" : $"Wrong! Expected {_currentQuestion.answer}");
            return correct;
        }

        /// <summary>
        /// Returns the text of the current question for UI display.
        /// </summary>
        /// <returns>The current question's text, or empty string if none.</returns>
        public string GetCurrentQuestionText()
        {
            return _currentQuestion != null ? _currentQuestion.text : "";
        }
    }
}