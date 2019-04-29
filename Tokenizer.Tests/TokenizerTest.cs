﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tokens.Exceptions;
using Tokens.Parsers;

namespace Tokens
{
    [TestFixture]
    public class TokenizerTest
    {
        private Tokenizer tokenizer;

        private class TestClass
        {
            public string Message { get; set; }

            public string Name { get; set; }

            public int Counter { get; set; }

            public IList<string> List { get; set; }

            public TestClass Nested { get; set; }
        }

        private class Student
        {
            public string FirstName { get; set; }

            public string MiddleName { get; set; }

            public string LastName { get; set; }

            public DateTime Enrolled { get; set; }

            public int Number { get; set; }
        }

        private class Teacher : Student
        {
            public List<string> Class { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            tokenizer = new Tokenizer();
        }

        [Test]
        public void TestExtractSingleValue()
        {
            const string pattern = @"First Name: {FirstName}";
            const string input = @"First Name: Alice";

            var student = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Alice", student.FirstName);
        }

        [Test]
        public void TestExtractTwoValues()
        {
            const string pattern = @"First Name: {Student.FirstName}, Last Name: {Student.LastName}";
            const string input = @"First Name: Alice, Last Name: Smith";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractThreeValues()
        {
            const string pattern = @"First Name: {Student.FirstName}, Middle Name: {Student.MiddleName}, Last Name: {Student.LastName}";
            const string input = @"First Name: Alice, Middle Name: Roberta, Last Name: Smith";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
            Assert.AreEqual("Roberta", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractThreeValuesWithLineBreaks()
        {
            const string pattern = 
@"---
# Tokens must appear in defined order
OutOfOrder: false
---
First Name: {FirstName}
Middle Name: {MiddleName}
Last Name: {LastName}";
            const string input = 
@"First Name: Alice
Middle Name: Roberta
Last Name: Smith";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
            Assert.AreEqual("Roberta", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractMultilineValues()
        {
            const string pattern = 
@"Comments:
{FirstName}

Name:
{LastName}";
            const string input = 
@"Comments:
Everything went well,
we had a nice time.

Name:
Bob";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Everything went well,\nwe had a nice time.", employee.FirstName);
            Assert.AreEqual("Bob", employee.LastName);
        }

        [Test]
        public void TestExtractValueWithTrailingTemplate()
        {
            const string pattern = @"First Name: {Student.FirstName}, Role: Programmer";
            const string input = @"First Name: Alice, Role: Programmer";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
        }

        [Test]
        public void TestExtractValueWithChangesType()
        {
            const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number}";
            const string input = @"First Name: Bob, Number: 12345";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(12345, employee.Number);
        }

        [Test]
        public void TestExtractValueWithDateTime()
        {
            const string pattern = @"First Name: {FirstName}, Last Name: {LastName}, Enrolled: {Enrolled:ToDateTime('dd MMM yyyy')}";
            const string input = @"First Name: Alice, Last Name: Smith, Enrolled: 16 Jan 2018";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
            Assert.AreEqual("Smith", employee.LastName);
            Assert.AreEqual(new DateTime(2018, 1, 16), employee.Enrolled);
        }

        [Test]
        public void TestExtractValueWithValidatorWhenValid()
        {
            const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number:IsNumeric}";
            const string input = @"First Name: Bob, Number: 12345";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(12345, employee.Number);
        }

        [Test]
        public void TestExtractValueWithValidatorWhenInvalid()
        {
            const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number:IsNumeric}";
            const string input = @"First Name: Bob, Number: Not a number";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(0, employee.Number);
        }

        [Test]
        public void TestExtractValueWithValidatorWhenInvalidPicksNextValidMatch()
        {
            const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number:IsNumeric}";
            const string input = @"First Name: Bob, Number: (not a number), Number: 67890";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(67890, employee.Number);
        }

        [Test]
        public void TestExtractOptionalValueWhenNotPresent()
        {
            const string pattern = @"First Name: {Student.FirstName}, Middle Name: {Student.MiddleName?}, Last Name: {Student.LastName}";
            const string input = @"First Name: Bob, Last Name: Smith";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractOptionalValueWhenPresent()
        {
            const string pattern = @"First Name: {Student.FirstName}, Middle Name: {Student.MiddleName?}, Last Name: {Student.LastName}";
            const string input = @"First Name: Bob, Middle Name: Charles, Last Name: Smith";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual("Charles", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractWhenOperatorMissing()
        {
            const string pattern = "Hello {Student.FirstName:UnknownFunction} World";
            const string input = "Hello ... World";

            Assert.Throws<TokenizerException>(() => tokenizer.Parse<Student>(pattern, input));            
        }

        [Test]
        public void TestExtractWhenNotPresentInInput()
        {
            const string pattern = "First Name: {Student.FirstName}";
            const string input = "David";

            var result = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual(result.FirstName, null);
        }

        [Test]
        public void TestExtractListValues()
        {
            const string pattern = "Student: {Teacher.Class*$}";
            const string input = "Student: Alice\r\nStudent: Bob";

            var result = tokenizer.Parse<Teacher>(pattern, input);

            Assert.AreEqual(2, result.Class.Count);
            Assert.AreEqual("Alice", result.Class[0]);
            Assert.AreEqual("Bob", result.Class[1]);            
        }

        [Test]
        public void TestExtractListValuesOnNewLines()
        {
            var pattern = 
            @"Name: {FirstName}
            Student: {Class*}
            Number: {Number}";
            var input = 
            @"Name: Sue
            Student: Alice
            Student: Bob
            Student: Charles
            Number: 1234";

            var result = tokenizer.Parse<Teacher>(pattern, input);

            Assert.AreEqual("Sue", result.FirstName);
            Assert.AreEqual(3, result.Class.Count);
            Assert.AreEqual("Alice", result.Class[0]);
            Assert.AreEqual("Bob", result.Class[1]);            
            Assert.AreEqual("Charles", result.Class[2]);
            Assert.AreEqual(1234, result.Number);
        }

        [Test]
        public void TestExtractEmbeddedListValues()
        {
            const string pattern = "Name: {Teacher.FirstName}, Student: {Teacher.Class*}, Number: {Teacher.Number}";
            const string input = "Name: Alice, Student: Bob, Student: Sue, Number: 1234";

            var result = tokenizer.Parse<Teacher>(pattern, input);

            Assert.AreEqual("Alice", result.FirstName);
            Assert.AreEqual(2, result.Class.Count);
            Assert.AreEqual("Bob", result.Class[0]);
            Assert.AreEqual("Sue", result.Class[1]);            
            Assert.AreEqual(1234, result.Number);
        }

        [Test]
        public void TestExtractDoesntThrowErrorWhenOptionsSetToFalse()
        {
            const string pattern = "Hello {TestClass.MissingPropertyName}";
            const string input = "Hello World";

            tokenizer.Options.ThrowExceptionOnMissingProperty = false;

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.IsNotNull(result);
        }

        [Test]
        public void TestExtracsThrowsAnErrorWhenOptionsSetToTrue()
        {
            const string pattern = "Hello {TestClass.MissingPropertyName}";
            const string input = "Hello World";

            tokenizer.Options.ThrowExceptionOnMissingProperty = true;

            Assert.Throws<MissingMemberException>(() => tokenizer.Parse<TestClass>(pattern, input));
        }

        [Test]
        public void TestExtractMulitplsStopsExtractingOnEmptyLine()
        {
            const string pattern = @"
Name servers:
        {TestClass.List*}
        {TestClass.List*}

    WHOIS lookup made at 10:35:59 22-Oct-2014";
            const string input = @"
Name servers:
        ns1.rbsov.bbc.co.uk       212.58.241.67
        ns1.tcams.bbc.co.uk       212.72.49.3
        ns1.thdow.bbc.co.uk       212.58.240.163

    WHOIS lookup made at 10:35:59 22-Oct-2014";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(3, result.List.Count);
        }

        [Test]
        public void TestExtractValueWithMismatchedNewLines()
        {
            const string pattern = "First Name:\n{Student.FirstName}";
            const string input = "First Name:\r\nAlice";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
        }

        [Test]
        public void TestExtractValueWithAnyOrder()
        {
            const string pattern = @"---
OutOfOrder: true
---
First Name: {Student.FirstName}
Middle Name: {Student.MiddleName}
Last Name: {Student.LastName}";
            const string input = @"Last Name: Smith
First Name: Bob
Middle Name: Charles";

            var employee = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual("Charles", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractPatternTwice()
        {
            const string pattern = @"First Name: {Student.FirstName}, Last Name: {Student.LastName}";
            const string input = @"First Name: Alice, Last Name: Smith";

            var template = new TokenParser().Parse(pattern);

            var one = tokenizer.Parse<Student>(template, input);

            Assert.AreEqual("Alice", one.FirstName);
            Assert.AreEqual("Smith", one.LastName);

            var two = tokenizer.Parse<Student>(template, input);

            Assert.AreEqual("Alice", two.FirstName);
            Assert.AreEqual("Smith", two.LastName);
        }

        [Test]
        public void TestTrimTrailingWhiteSpace()
        {
            // Front matter configuration
            const string pattern = @"---
# Trim Whitespace
TrimTrailingWhitespace: true
---
First Name: {FirstName}
Last Name: {LastName}
...";

            const string input = "First Name: John    ";

            // Should get overridden by embedded pattern declaration
            tokenizer.Options.TrimTrailingWhiteSpace = false;

            var student = tokenizer.Parse<Student>(pattern, input);

            Assert.AreEqual("John", student.FirstName);
        }

        [Test]
        public void TestParseIsNumericValidator()
        {
            const string pattern = "Age: {Age:IsNumeric}";
            const string input = "Age: Ten, Age: 11";

            var person = new Tokenizer().Parse<TokenTest.Person>(pattern, input);

            Assert.AreEqual(person.Age, 11);
        }
    }
}

